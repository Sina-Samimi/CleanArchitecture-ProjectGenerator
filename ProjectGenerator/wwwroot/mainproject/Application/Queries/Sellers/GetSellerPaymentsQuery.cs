using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Sellers;
using MobiRooz.Application.Interfaces;
using MobiRooz.Application.Queries.Admin.FinancialSettings;
using MobiRooz.Domain.Enums;
using MobiRooz.SharedKernel.BaseTypes;
using MediatR;

namespace MobiRooz.Application.Queries.Sellers;

public sealed record GetSellerPaymentsQuery(string SellerId) : IQuery<SellerPaymentsDto>
{
    public sealed class Handler : IQueryHandler<GetSellerPaymentsQuery, SellerPaymentsDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly ISellerProfileRepository _sellerProfileRepository;
        private readonly IMediator _mediator;

        public Handler(
            IProductRepository productRepository,
            IInvoiceRepository invoiceRepository,
            IWalletRepository walletRepository,
            ISellerProfileRepository sellerProfileRepository,
            IMediator mediator)
        {
            _productRepository = productRepository;
            _invoiceRepository = invoiceRepository;
            _walletRepository = walletRepository;
            _sellerProfileRepository = sellerProfileRepository;
            _mediator = mediator;
        }

        public async Task<Result<SellerPaymentsDto>> Handle(GetSellerPaymentsQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SellerId))
            {
                return Result<SellerPaymentsDto>.Failure("شناسه فروشنده معتبر نیست.");
            }

            // دریافت محصولات فروشنده
            var productDtos = await _productRepository.GetBySellerAsync(request.SellerId, cancellationToken);
            var productIds = productDtos.Select(p => p.Id).ToList();

            if (productIds.Count == 0)
            {
                return Result<SellerPaymentsDto>.Success(new SellerPaymentsDto(
                    TotalRevenue: 0,
                    PaidRevenue: 0,
                    PendingRevenue: 0,
                    TotalInvoices: 0,
                    PaidInvoices: 0,
                    PendingInvoices: 0,
                    Invoices: new List<SellerInvoiceDto>(),
                    GeneratedAt: DateTimeOffset.UtcNow));
            }

            // دریافت تمام فاکتورها
            var allInvoices = await _invoiceRepository.GetListAsync(null, cancellationToken);
            var sellerInvoices = new List<Domain.Entities.Billing.Invoice>();
            
            foreach (var invoice in allInvoices)
            {
                if (invoice.Items.Any(item => 
                    item.ItemType == InvoiceItemType.Product && 
                    item.ReferenceId.HasValue && 
                    productIds.Contains(item.ReferenceId.Value)))
                {
                    sellerInvoices.Add(invoice);
                }
            }

            // دریافت تنظیمات مالی برای محاسبه سهم فروشنده (مقدار پیش‌فرض)
            var financialSettingsResult = await _mediator.Send(new GetFinancialSettingsQuery(), cancellationToken);
            var defaultSellerSharePercentage = financialSettingsResult.IsSuccess && financialSettingsResult.Value is not null
                ? financialSettingsResult.Value.SellerProductSharePercentage
                : 0m;
            var calculationMethod = financialSettingsResult.IsSuccess && financialSettingsResult.Value is not null
                ? financialSettingsResult.Value.CommissionCalculationMethod
                : PlatformCommissionCalculationMethod.Complementary;
            var platformCommissionPercentage = financialSettingsResult.IsSuccess && financialSettingsResult.Value is not null
                ? financialSettingsResult.Value.PlatformCommissionPercentage
                : 0m;

            // دریافت درصد فروش فروشنده (اگر تنظیم شده باشد، در غیر این صورت از تنظیمات مالی استفاده می‌شود)
            var sellerProfile = await _sellerProfileRepository.GetByUserIdAsync(request.SellerId, cancellationToken);
            var sellerSharePercentage = sellerProfile?.SellerSharePercentage ?? defaultSellerSharePercentage;

            // دریافت تراکنش‌های کیف پول فروشنده (seller share)
            var wallet = await _walletRepository.GetByUserIdWithTransactionsAsync(request.SellerId, null, cancellationToken);
            var sellerShareTransactions = wallet?.Transactions
                .Where(t => t.Type == WalletTransactionType.Credit &&
                           t.Status == TransactionStatus.Succeeded &&
                           t.InvoiceId.HasValue &&
                           !string.IsNullOrWhiteSpace(t.Metadata) &&
                           t.Metadata.Contains("SELLER_SHARE", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<Domain.Entities.Billing.WalletTransaction>();

            // ایجاد دیکشنری برای دسترسی سریع به تراکنش‌های seller share بر اساس InvoiceId
            var sellerShareByInvoice = sellerShareTransactions
                .GroupBy(t => t.InvoiceId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            // محاسبه درآمدها
            decimal totalRevenue = 0;
            decimal paidRevenue = 0;
            decimal pendingRevenue = 0;

            var invoiceDtos = new List<SellerInvoiceDto>();

            foreach (var invoice in sellerInvoices.OrderByDescending(i => i.IssueDate))
            {
                var sellerItems = invoice.Items
                    .Where(item => item.ItemType == InvoiceItemType.Product && 
                                   item.ReferenceId.HasValue && 
                                   productIds.Contains(item.ReferenceId.Value))
                    .ToList();
                
                // محاسبه مبلغ کل فاکتور برای این فروشنده
                var invoiceTotalAmount = sellerItems.Sum(item => item.Total);
                
                // محاسبه سهم فروشنده - همیشه از کل سهم فروشنده محاسبه می‌کنیم
                // این باعث می‌شود: درآمد کل = موجودی کیف پول + درآمدهای در انتظار
                decimal sellerShareAmount = 0;
                if (sellerSharePercentage > 0)
                {
                    if (calculationMethod == PlatformCommissionCalculationMethod.DeductFromSeller)
                    {
                        var baseShare = invoiceTotalAmount * sellerSharePercentage / 100m;
                        var platformCommission = invoiceTotalAmount * platformCommissionPercentage / 100m;
                        sellerShareAmount = decimal.Round(baseShare - platformCommission, 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        sellerShareAmount = decimal.Round(invoiceTotalAmount * sellerSharePercentage / 100m, 2, MidpointRounding.AwayFromZero);
                    }
                }

                totalRevenue += sellerShareAmount;

                if (invoice.Status == InvoiceStatus.Paid)
                {
                    paidRevenue += sellerShareAmount;
                }
                else if (invoice.Status == InvoiceStatus.Pending || invoice.Status == InvoiceStatus.PartiallyPaid)
                {
                    pendingRevenue += sellerShareAmount;
                }

                invoiceDtos.Add(new SellerInvoiceDto(
                    InvoiceId: invoice.Id,
                    InvoiceNumber: invoice.InvoiceNumber,
                    Title: invoice.Title,
                    TotalAmount: sellerShareAmount, // نمایش سهم فروشنده به جای مبلغ کل
                    PaidAmount: invoice.Status == InvoiceStatus.Paid ? sellerShareAmount : (invoice.Status == InvoiceStatus.PartiallyPaid ? (sellerShareAmount * invoice.PaidAmount / invoiceTotalAmount) : 0),
                    PendingAmount: invoice.Status == InvoiceStatus.Paid ? 0 : (sellerShareAmount - (invoice.Status == InvoiceStatus.PartiallyPaid ? (sellerShareAmount * invoice.PaidAmount / invoiceTotalAmount) : 0)),
                    Status: invoice.Status.ToString(),
                    IssueDate: invoice.IssueDate,
                    DueDate: invoice.DueDate,
                    ProductCount: sellerItems.Count));
            }

            var stats = new SellerPaymentsDto(
                TotalRevenue: totalRevenue,
                PaidRevenue: paidRevenue,
                PendingRevenue: pendingRevenue,
                TotalInvoices: sellerInvoices.Count,
                PaidInvoices: sellerInvoices.Count(i => i.Status == InvoiceStatus.Paid),
                PendingInvoices: sellerInvoices.Count(i => i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.PartiallyPaid),
                Invoices: invoiceDtos,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Result<SellerPaymentsDto>.Success(stats);
        }
    }
}

