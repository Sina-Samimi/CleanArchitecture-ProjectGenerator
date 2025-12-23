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

public sealed record GetSellerDashboardStatsQuery(string SellerId) : IQuery<SellerDashboardStatsDto>
{
    public sealed class Handler : IQueryHandler<GetSellerDashboardStatsQuery, SellerDashboardStatsDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IProductCommentRepository _commentRepository;
        private readonly IProductCustomRequestRepository _customRequestRepository;
        private readonly IShipmentTrackingRepository _shipmentTrackingRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly ISellerProfileRepository _sellerProfileRepository;
        private readonly IMediator _mediator;

        public Handler(
            IProductRepository productRepository,
            IInvoiceRepository invoiceRepository,
            IProductCommentRepository commentRepository,
            IProductCustomRequestRepository customRequestRepository,
            IShipmentTrackingRepository shipmentTrackingRepository,
            IWalletRepository walletRepository,
            ISellerProfileRepository sellerProfileRepository,
            IMediator mediator)
        {
            _productRepository = productRepository;
            _invoiceRepository = invoiceRepository;
            _commentRepository = commentRepository;
            _customRequestRepository = customRequestRepository;
            _shipmentTrackingRepository = shipmentTrackingRepository;
            _walletRepository = walletRepository;
            _sellerProfileRepository = sellerProfileRepository;
            _mediator = mediator;
        }

        public async Task<Result<SellerDashboardStatsDto>> Handle(GetSellerDashboardStatsQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SellerId))
            {
                return Result<SellerDashboardStatsDto>.Failure("شناسه فروشنده معتبر نیست.");
            }

            // دریافت محصولات فروشنده
            var productDtos = await _productRepository.GetBySellerAsync(request.SellerId, cancellationToken);
            var productIds = productDtos.Select(p => p.Id).ToList();

            // دریافت entity های محصولات برای آمار دقیق‌تر
            var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
            var activeProducts = products.Where(p => !p.IsDeleted).ToList();

            // آمار محصولات
            var totalProducts = activeProducts.Count;
            var publishedProducts = activeProducts.Count(p => p.IsPublished);
            var pendingProducts = activeProducts.Count(p => !p.IsPublished);
            // محصولات منتشر نشده به عنوان pending در نظر گرفته می‌شوند
            var draftProducts = 0; // Product entity دارای IsDraft نیست

            // محصولات کم‌موجودی
            var lowStockProducts = activeProducts
                .Where(p => p.TrackInventory && p.StockQuantity < 5)
                .Select(p => new LowStockProductDto(p.Id, p.Name, p.StockQuantity))
                .ToList();

            // آمار سفارشات
            var allInvoices = await _invoiceRepository.GetListAsync(null, cancellationToken);
            var sellerOrders = new List<Domain.Entities.Billing.Invoice>();
            
            foreach (var invoice in allInvoices)
            {
                if (invoice.Items.Any(item => 
                    item.ItemType == InvoiceItemType.Product && 
                    item.ReferenceId.HasValue && 
                    productIds.Contains(item.ReferenceId.Value)))
                {
                    sellerOrders.Add(invoice);
                }
            }

            var totalOrders = sellerOrders.Count;
            var newOrdersCount = sellerOrders.Count(o => o.IssueDate >= DateTimeOffset.UtcNow.AddHours(-24));
            var paidOrders = sellerOrders.Count(o => o.Status == InvoiceStatus.Paid);
            var pendingOrders = sellerOrders.Count(o => o.Status == InvoiceStatus.Pending || o.Status == InvoiceStatus.PartiallyPaid);

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

            // محاسبه درآمد کل از سفارشات پرداخت شده (بر اساس سهم فروشنده)
            // همیشه از کل سهم فروشنده محاسبه می‌کنیم، نه فقط مبلغ واریز شده
            decimal totalRevenue = 0;
            foreach (var invoice in sellerOrders.Where(o => o.Status == InvoiceStatus.Paid))
            {
                var sellerItems = invoice.Items
                    .Where(item => item.ItemType == InvoiceItemType.Product && 
                                   item.ReferenceId.HasValue && 
                                   productIds.Contains(item.ReferenceId.Value))
                    .ToList();
                
                var invoiceTotalAmount = sellerItems.Sum(item => item.Total);
                
                // همیشه از کل سهم فروشنده محاسبه کن (نه فقط مبلغ واریز شده)
                // این باعث می‌شود: درآمد کل = موجودی کیف پول + درآمدهای در انتظار
                if (sellerSharePercentage > 0)
                {
                    decimal sellerShare;
                    if (calculationMethod == PlatformCommissionCalculationMethod.DeductFromSeller)
                    {
                        var baseShare = invoiceTotalAmount * sellerSharePercentage / 100m;
                        var platformCommission = invoiceTotalAmount * platformCommissionPercentage / 100m;
                        sellerShare = decimal.Round(baseShare - platformCommission, 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        sellerShare = decimal.Round(invoiceTotalAmount * sellerSharePercentage / 100m, 2, MidpointRounding.AwayFromZero);
                    }
                    totalRevenue += sellerShare;
                }
            }

            // آمار نظرات
            var allComments = new List<Domain.Entities.Catalog.ProductComment>();
            foreach (var productId in productIds)
            {
                var comments = await _commentRepository.GetByProductIdAsync(productId, cancellationToken);
                allComments.AddRange(comments);
            }
            var approvedComments = allComments.Where(c => c.IsApproved && !c.IsDeleted).ToList();
            var newProductComments = allComments.Where(c => !c.IsApproved && !c.IsDeleted).Count();
            var approvedProductComments = approvedComments.Count;
            var totalComments = approvedComments.Count;
            var pendingReplyComments = approvedComments.Count(c => c.ParentId == null && !approvedComments.Any(r => r.ParentId == c.Id));

            // آمار درخواست‌های سفارشی
            var allCustomRequests = new List<Domain.Entities.Catalog.ProductCustomRequest>();
            foreach (var productId in productIds)
            {
                var requests = await _customRequestRepository.GetByProductIdAsync(productId, cancellationToken);
                allCustomRequests.AddRange(requests);
            }
            var totalCustomRequests = allCustomRequests.Count(c => !c.IsDeleted);
            var pendingCustomRequests = allCustomRequests.Count(c => !c.IsDeleted && c.Status == CustomRequestStatus.Pending);

            // آمار پیگیری ارسال
            var sellerTrackings = new List<Domain.Entities.Orders.ShipmentTracking>();
            foreach (var productId in productIds)
            {
                var trackings = await _shipmentTrackingRepository.GetByProductIdAsync(productId, cancellationToken);
                sellerTrackings.AddRange(trackings);
            }
            var totalShipments = sellerTrackings.Count(t => !t.IsDeleted);
            var preparingShipments = sellerTrackings.Count(t => !t.IsDeleted && t.Status == ShipmentStatus.Preparing);
            var shippedShipments = sellerTrackings.Count(t => !t.IsDeleted && t.Status == ShipmentStatus.Shipped);
            var deliveredShipments = sellerTrackings.Count(t => !t.IsDeleted && t.Status == ShipmentStatus.Delivered);

            var stats = new SellerDashboardStatsDto(
                totalProducts,
                publishedProducts,
                pendingProducts,
                draftProducts,
                totalOrders,
                newOrdersCount,
                paidOrders,
                pendingOrders,
                totalRevenue,
                totalComments,
                pendingReplyComments,
                totalCustomRequests,
                pendingCustomRequests,
                totalShipments,
                preparingShipments,
                shippedShipments,
                deliveredShipments,
                newProductComments,
                approvedProductComments,
                lowStockProducts,
                DateTimeOffset.UtcNow);

            return Result<SellerDashboardStatsDto>.Success(stats);
        }
    }
}

