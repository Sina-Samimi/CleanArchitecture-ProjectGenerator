using System;
using System.Collections.Generic;
using System.Linq;

namespace LogsDtoCloneTest.WebSite.Areas.Seller.Models;

public sealed class SellerStatisticsViewModel
{
    public int TotalProducts { get; set; }
    public int PublishedProducts { get; set; }
    public int PendingProducts { get; set; }
    public int TotalViews { get; set; }
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public int PendingOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalComments { get; set; }
    public int ApprovedComments { get; set; }
    public int PendingReplyComments { get; set; }
    public int TotalCustomRequests { get; set; }
    public int PendingCustomRequests { get; set; }
    public int TotalShipments { get; set; }
    public int PreparingShipments { get; set; }
    public int ShippedShipments { get; set; }
    public int DeliveredShipments { get; set; }
    public List<ProductStatisticsViewModel> TopProducts { get; set; } = new();
    public List<OrderStatisticsViewModel> RecentOrders { get; set; } = new();
    public List<DailyViewViewModel> DailyViews { get; set; } = new();
    public DateTimeOffset GeneratedAt { get; set; }
}

public sealed class ProductStatisticsViewModel
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public bool IsPublished { get; set; }
}

public sealed class OrderStatisticsViewModel
{
    public Guid OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset OrderDate { get; set; }
}

public sealed class DailyViewViewModel
{
    public DateOnly Date { get; set; }
    public int ViewCount { get; set; }
}

