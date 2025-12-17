using System;
using System.Collections.Generic;

namespace Attar.WebSite.Areas.Seller.Models;

public sealed class SellerDashboardViewModel
{
    public int TotalProducts { get; set; }

    public int PublishedProducts { get; set; }

    public int PendingProducts { get; set; }

    public int DraftProducts { get; set; }

    public int TotalOrders { get; set; }

    public int NewOrdersCount { get; set; }

    public int PaidOrders { get; set; }

    public int PendingOrders { get; set; }

    public decimal TotalRevenue { get; set; }

    public int TotalComments { get; set; }

    public int PendingReplyComments { get; set; }

    public int TotalCustomRequests { get; set; }

    public int PendingCustomRequests { get; set; }

    public int TotalShipments { get; set; }

    public int PreparingShipments { get; set; }

    public int ShippedShipments { get; set; }

    public int DeliveredShipments { get; set; }

    public int NewProductCommentsCount { get; set; }

    public int ApprovedProductCommentsCount { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }

    public IReadOnlyCollection<LowStockProductViewModel> LowStockProducts { get; set; } = Array.Empty<LowStockProductViewModel>();
}

public sealed class LowStockProductViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}

