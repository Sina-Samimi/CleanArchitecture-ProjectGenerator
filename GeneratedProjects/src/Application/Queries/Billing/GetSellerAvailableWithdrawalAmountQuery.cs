using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Application.Queries.Admin.FinancialSettings;
using TestAttarClone.Application.Queries.Sellers;
using TestAttarClone.Domain.Entities.Billing;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.BaseTypes;
using MediatR;

namespace TestAttarClone.Application.Queries.Billing;

public sealed record GetSellerAvailableWithdrawalAmountQuery(string SellerId) : IQuery<SellerAvailableWithdrawalAmountDto>;

public sealed record SellerAvailableWithdrawalAmountDto(
    decimal WalletBalance,
    decimal PendingRevenue,
    decimal TotalAvailable,
    string Currency);

public sealed class GetSellerAvailableWithdrawalAmountQueryHandler : IQueryHandler<GetSellerAvailableWithdrawalAmountQuery, SellerAvailableWithdrawalAmountDto>
{
    private readonly IMediator _mediator;
    private readonly IWalletRepository _walletRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ISellerProfileRepository _sellerProfileRepository;

    public GetSellerAvailableWithdrawalAmountQueryHandler(
        IMediator mediator,
        IWalletRepository walletRepository,
        IProductRepository productRepository,
        IInvoiceRepository invoiceRepository,
        ISellerProfileRepository sellerProfileRepository)
    {
        _mediator = mediator;
        _walletRepository = walletRepository;
        _productRepository = productRepository;
        _invoiceRepository = invoiceRepository;
        _sellerProfileRepository = sellerProfileRepository;
    }

    public async Task<Result<SellerAvailableWithdrawalAmountDto>> Handle(GetSellerAvailableWithdrawalAmountQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SellerId))
        {
            return Result<SellerAvailableWithdrawalAmountDto>.Failure("شناسه فروشنده معتبر نیست.");
        }

        var sellerId = request.SellerId.Trim();

        // دریافت موجودی کیف پول
        var wallet = await _walletRepository.GetByUserIdWithTransactionsAsync(sellerId, null, cancellationToken);
        var walletBalance = wallet?.Balance ?? 0m;
        var currency = wallet?.Currency ?? "IRT";

        // دریافت محصولات فروشنده
        var productDtos = await _productRepository.GetBySellerAsync(sellerId, cancellationToken);
        var productIds = productDtos.Select(p => p.Id).ToList();

        decimal pendingRevenue = 0m;

        if (productIds.Count > 0)
        {
            // دریافت تنظیمات مالی (مقدار پیش‌فرض)
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
            var sellerProfile = await _sellerProfileRepository.GetByUserIdAsync(sellerId, cancellationToken);
            var sellerSharePercentage = sellerProfile?.SellerSharePercentage ?? defaultSellerSharePercentage;

            // دریافت تراکنش‌های کیف پول که seller share هستند
            var sellerShareTransactions = wallet?.Transactions
                .Where(t => t.Type == WalletTransactionType.Credit &&
                           t.Status == TransactionStatus.Succeeded &&
                           t.InvoiceId.HasValue &&
                           !string.IsNullOrWhiteSpace(t.Metadata) &&
                           t.Metadata.Contains("SELLER_SHARE", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<WalletTransaction>();

            var sellerShareByInvoice = sellerShareTransactions
                .GroupBy(t => t.InvoiceId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            // دریافت تمام فاکتورها
            var allInvoices = await _invoiceRepository.GetListAsync(null, cancellationToken);
            var sellerInvoices = allInvoices
                .Where(invoice => invoice.Items.Any(item =>
                    item.ItemType == InvoiceItemType.Product &&
                    item.ReferenceId.HasValue &&
                    productIds.Contains(item.ReferenceId.Value)))
                .ToList();

            // محاسبه درآمدهای در انتظار
            foreach (var invoice in sellerInvoices)
            {
                var sellerItems = invoice.Items
                    .Where(item => item.ItemType == InvoiceItemType.Product &&
                                   item.ReferenceId.HasValue &&
                                   productIds.Contains(item.ReferenceId.Value))
                    .ToList();

                var invoiceTotalAmount = sellerItems.Sum(item => item.Total);

                // محاسبه سهم فروشنده
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

                // اگر فاکتور پرداخت شده اما seller share به کیف پول واریز نشده، به pending revenue اضافه کن
                if (invoice.Status == InvoiceStatus.Paid)
                {
                    var creditedAmount = sellerShareByInvoice.GetValueOrDefault(invoice.Id, 0m);
                    if (creditedAmount < sellerShareAmount)
                    {
                        pendingRevenue += sellerShareAmount - creditedAmount;
                    }
                }
                // اگر فاکتور پرداخت نشده، کل seller share به pending revenue اضافه می‌شود
                else if (invoice.Status == InvoiceStatus.Pending || invoice.Status == InvoiceStatus.PartiallyPaid)
                {
                    pendingRevenue += sellerShareAmount;
                }
            }
        }

        var totalAvailable = walletBalance + pendingRevenue;

        var dto = new SellerAvailableWithdrawalAmountDto(
            WalletBalance: walletBalance,
            PendingRevenue: pendingRevenue,
            TotalAvailable: totalAvailable,
            Currency: currency);

        return Result<SellerAvailableWithdrawalAmountDto>.Success(dto);
    }
}

