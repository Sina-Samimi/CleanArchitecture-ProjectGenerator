using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Sellers;
using Attar.Application.Interfaces;
using Attar.Application.Queries.Admin.FinancialSettings;
using Attar.Domain.Enums;
using Attar.SharedKernel.BaseTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Attar.Application.Queries.Sellers;

public sealed record GetSellerStatisticsQuery(string SellerId) : IQuery<SellerStatisticsDto>
{
    public sealed class Handler : IQueryHandler<GetSellerStatisticsQuery, SellerStatisticsDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IProductCommentRepository _commentRepository;
        private readonly IProductCustomRequestRepository _customRequestRepository;
        private readonly IShipmentTrackingRepository _shipmentTrackingRepository;
        private readonly IVisitRepository _visitRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly ISellerProfileRepository _sellerProfileRepository;
        private readonly IMediator _mediator;

        public Handler(
            IProductRepository productRepository,
            IInvoiceRepository invoiceRepository,
            IProductCommentRepository commentRepository,
            IProductCustomRequestRepository customRequestRepository,
            IShipmentTrackingRepository shipmentTrackingRepository,
            IVisitRepository visitRepository,
            IWalletRepository walletRepository,
            ISellerProfileRepository sellerProfileRepository,
            IMediator mediator)
        {
            _productRepository = productRepository;
            _invoiceRepository = invoiceRepository;
            _commentRepository = commentRepository;
            _customRequestRepository = customRequestRepository;
            _shipmentTrackingRepository = shipmentTrackingRepository;
            _visitRepository = visitRepository;
            _walletRepository = walletRepository;
            _sellerProfileRepository = sellerProfileRepository;
            _mediator = mediator;
        }

        public async Task<Result<SellerStatisticsDto>> Handle(GetSellerStatisticsQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SellerId))
            {
                return Result<SellerStatisticsDto>.Failure("شناسه فروشنده معتبر نیست.");
            }

            // دریافت محصولات فروشنده
            var productDtos = await _productRepository.GetBySellerAsync(request.SellerId, cancellationToken);
            var productIds = productDtos.Select(p => p.Id).ToList();

            if (productIds.Count == 0)
            {
                return Result<SellerStatisticsDto>.Success(new SellerStatisticsDto(
                    TotalProducts: 0,
                    PublishedProducts: 0,
                    PendingProducts: 0,
                    TotalViews: 0,
                    TotalOrders: 0,
                    PaidOrders: 0,
                    PendingOrders: 0,
                    TotalRevenue: 0,
                    TotalComments: 0,
                    ApprovedComments: 0,
                    PendingReplyComments: 0,
                    TotalCustomRequests: 0,
                    PendingCustomRequests: 0,
                    TotalShipments: 0,
                    PreparingShipments: 0,
                    ShippedShipments: 0,
                    DeliveredShipments: 0,
                    TopProducts: new List<ProductStatisticsDto>(),
                    RecentOrders: new List<OrderStatisticsDto>(),
                    DailyViews: new List<DailyViewDto>(),
                    GeneratedAt: DateTimeOffset.UtcNow));
            }

            // دریافت entity های محصولات
            var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
            var activeProducts = products.Where(p => !p.IsDeleted).ToList();

            // آمار محصولات
            var totalProducts = activeProducts.Count;
            var publishedProducts = activeProducts.Count(p => p.IsPublished);
            var pendingProducts = activeProducts.Count(p => !p.IsPublished);

            // محاسبه بازدیدهای کل محصولات
            var totalViews = 0;
            var productViews = new Dictionary<Guid, int>();
            foreach (var productId in productIds)
            {
                var views = await _visitRepository.GetProductVisitCountAsync(productId, cancellationToken);
                totalViews += views;
                productViews[productId] = views;
            }

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

            // محاسبه درآمد کل (بر اساس سهم فروشنده)
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
            var totalComments = allComments.Count(c => !c.IsDeleted);
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

            // محصولات برتر (بر اساس بازدید)
            var topProducts = productDtos
                .Select(p => new ProductStatisticsDto(
                    p.Id,
                    p.Name,
                    productViews.GetValueOrDefault(p.Id, 0),
                    p.IsPublished))
                .OrderByDescending(p => p.ViewCount)
                .Take(10)
                .ToList();

            // آخرین سفارشات
            var recentOrders = sellerOrders
                .OrderByDescending(o => o.IssueDate)
                .Take(10)
                .Select(o =>
                {
                    var sellerItems = o.Items
                        .Where(item => item.ItemType == InvoiceItemType.Product && 
                                       item.ReferenceId.HasValue && 
                                       productIds.Contains(item.ReferenceId.Value))
                        .ToList();
                    var orderTotal = sellerItems.Sum(item => item.Total);
                    return new OrderStatisticsDto(
                        o.Id,
                        o.InvoiceNumber,
                        orderTotal,
                        o.Status.ToString(),
                        o.IssueDate);
                })
                .ToList();

            // آمار بازدید روزانه (آخرین 30 روز)
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
            var fromDate = today.AddDays(-29);
            var dailyProductVisits = await _visitRepository.GetDailyProductVisitsAsync(productIds, fromDate, today, cancellationToken);
            
            // ایجاد لیست کامل 30 روز (حتی اگر بازدیدی نباشد)
            var dailyViewsDict = dailyProductVisits.ToDictionary(d => d.Date, d => d.VisitCount);
            var dailyViews = new List<DailyViewDto>();
            for (int i = 29; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var viewCount = dailyViewsDict.GetValueOrDefault(date, 0);
                dailyViews.Add(new DailyViewDto(date, viewCount));
            }

            var stats = new SellerStatisticsDto(
                TotalProducts: totalProducts,
                PublishedProducts: publishedProducts,
                PendingProducts: pendingProducts,
                TotalViews: totalViews,
                TotalOrders: totalOrders,
                PaidOrders: paidOrders,
                PendingOrders: pendingOrders,
                TotalRevenue: totalRevenue,
                TotalComments: totalComments,
                ApprovedComments: approvedComments.Count,
                PendingReplyComments: pendingReplyComments,
                TotalCustomRequests: totalCustomRequests,
                PendingCustomRequests: pendingCustomRequests,
                TotalShipments: totalShipments,
                PreparingShipments: preparingShipments,
                ShippedShipments: shippedShipments,
                DeliveredShipments: deliveredShipments,
                TopProducts: topProducts,
                RecentOrders: recentOrders,
                DailyViews: dailyViews,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Result<SellerStatisticsDto>.Success(stats);
        }
    }
}

