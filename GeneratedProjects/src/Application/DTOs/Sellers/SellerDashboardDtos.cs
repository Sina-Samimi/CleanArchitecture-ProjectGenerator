using System;
using System.Collections.Generic;

namespace TestAttarClone.Application.DTOs.Sellers;

public sealed record SellerDashboardStatsDto(
    int TotalProducts,
    int PublishedProducts,
    int PendingProducts,
    int DraftProducts,
    int TotalOrders,
    int NewOrdersCount,
    int PaidOrders,
    int PendingOrders,
    decimal TotalRevenue,
    int TotalComments,
    int PendingReplyComments,
    int TotalCustomRequests,
    int PendingCustomRequests,
    int TotalShipments,
    int PreparingShipments,
    int ShippedShipments,
    int DeliveredShipments,
    int NewProductCommentsCount,
    int ApprovedProductCommentsCount,
    IReadOnlyCollection<LowStockProductDto> LowStockProducts,
    DateTimeOffset GeneratedAt);

public sealed record LowStockProductDto(Guid Id, string Name, int StockQuantity);

