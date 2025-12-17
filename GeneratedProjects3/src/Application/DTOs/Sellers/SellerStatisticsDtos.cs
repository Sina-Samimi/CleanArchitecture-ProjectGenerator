using System;
using System.Collections.Generic;

namespace LogTableRenameTest.Application.DTOs.Sellers;

public sealed record SellerStatisticsDto(
    int TotalProducts,
    int PublishedProducts,
    int PendingProducts,
    int TotalViews,
    int TotalOrders,
    int PaidOrders,
    int PendingOrders,
    decimal TotalRevenue,
    int TotalComments,
    int ApprovedComments,
    int PendingReplyComments,
    int TotalCustomRequests,
    int PendingCustomRequests,
    int TotalShipments,
    int PreparingShipments,
    int ShippedShipments,
    int DeliveredShipments,
    IReadOnlyCollection<ProductStatisticsDto> TopProducts,
    IReadOnlyCollection<OrderStatisticsDto> RecentOrders,
    IReadOnlyCollection<DailyViewDto> DailyViews,
    DateTimeOffset GeneratedAt);

public sealed record ProductStatisticsDto(
    Guid ProductId,
    string ProductTitle,
    int ViewCount,
    bool IsPublished);

public sealed record OrderStatisticsDto(
    Guid OrderId,
    string InvoiceNumber,
    decimal TotalAmount,
    string Status,
    DateTimeOffset OrderDate);

public sealed record DailyViewDto(
    DateOnly Date,
    int ViewCount);

