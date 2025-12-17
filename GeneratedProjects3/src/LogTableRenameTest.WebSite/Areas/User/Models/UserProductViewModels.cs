using System;
using System.Collections.Generic;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.WebSite.Areas.User.Models;

public sealed class UserProductLibraryViewModel
{
    public required IReadOnlyCollection<UserProductLibraryMetricViewModel> Metrics { get; init; }

    public required IReadOnlyCollection<UserPurchasedProductViewModel> Purchases { get; init; }

    public required UserProductLibraryFilterViewModel Filter { get; init; }

    public int TotalPurchases { get; init; }

    public int FilteredPurchases { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(FilteredPurchases / (double)PageSize);

    public bool HasPurchases => Purchases.Count > 0;
}

public sealed class UserProductLibraryMetricViewModel
{
    public required string Icon { get; init; }

    public required string Label { get; init; }

    public required string Value { get; init; }

    public string? Description { get; init; }

    public string Tone { get; init; } = "primary";
}

public sealed class UserPurchasedProductViewModel
{
    public required Guid InvoiceId { get; init; }

    public required string InvoiceNumber { get; init; }

    public required Guid InvoiceItemId { get; init; }

    public Guid? ProductId { get; init; }

    public required string Name { get; init; }

    public string? Summary { get; init; }

    public string? CategoryName { get; init; }

    public required string Type { get; init; }

    public bool IsDigital { get; init; }

    public bool CanDownload { get; init; }

    public string Status { get; init; } = string.Empty;

    public string StatusBadgeClass { get; init; } = "badge bg-secondary-subtle text-secondary-emphasis";

    public DateTimeOffset PurchasedAt { get; init; }

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal Total { get; init; }

    public decimal InvoiceGrandTotal { get; init; }

    public decimal InvoicePaidAmount { get; init; }

    public decimal InvoiceOutstandingAmount { get; init; }

    public string? ThumbnailPath { get; init; }

    public string? DownloadUrl { get; init; }

    public ShipmentTrackingInfoViewModel? ShipmentTracking { get; init; }
}

public sealed class ShipmentTrackingInfoViewModel
{
    public required ShipmentStatus Status { get; init; }

    public string StatusText { get; init; } = string.Empty;

    public string StatusBadgeClass { get; init; } = string.Empty;

    public string? TrackingNumber { get; init; }

    public DateTimeOffset StatusDate { get; init; }

    public string? Notes { get; init; }
}

public sealed class UserProductLibraryFilterViewModel
{
    public string? Search { get; init; }

    public UserProductTypeFilter? Type { get; init; }

    public UserProductStatusFilter? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public sealed class UserProductLibraryFilterRequest
{
    public string? Search { get; init; }

    public UserProductTypeFilter? Type { get; init; }

    public UserProductStatusFilter? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public enum UserProductTypeFilter
{
    Digital = 0,
    Physical = 1
}

public enum UserProductStatusFilter
{
    Paid = 0,
    PartiallyPaid = 1,
    Pending = 2,
    Overdue = 3
}
