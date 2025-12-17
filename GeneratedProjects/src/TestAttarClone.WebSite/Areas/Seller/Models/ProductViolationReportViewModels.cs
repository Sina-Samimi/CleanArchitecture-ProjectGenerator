using System;
using System.Collections.Generic;

namespace TestAttarClone.WebSite.Areas.Seller.Models;

public sealed class SellerProductViolationReportListViewModel
{
    public IReadOnlyCollection<SellerProductViolationReportViewModel> Reports { get; init; }
        = Array.Empty<SellerProductViolationReportViewModel>();

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public bool? SelectedIsReviewed { get; init; }
}

public sealed class SellerProductViolationReportViewModel
{
    public Guid Id { get; init; }

    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string? ProductSellerId { get; init; }

    public Guid? ProductOfferId { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string ReporterPhone { get; init; } = string.Empty;

    public bool IsReviewed { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ReviewedAt { get; init; }
}

