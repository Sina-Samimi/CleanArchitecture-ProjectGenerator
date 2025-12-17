using System;
using System.Collections.Generic;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed class ProductViolationReportListViewModel
{
    public IReadOnlyCollection<ProductViolationReportViewModel> Reports { get; init; }
        = Array.Empty<ProductViolationReportViewModel>();

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public bool? SelectedIsReviewed { get; init; }

    public Guid? SelectedProductId { get; init; }
    
    public string? SelectedSellerId { get; init; }

    public string? SelectedReporterPhone { get; init; }

    public string? SelectedSubject { get; init; }

    public DateTimeOffset? SelectedDateFrom { get; init; }

    public DateTimeOffset? SelectedDateTo { get; init; }

    // Persian-formatted date strings (e.g. "1402/05/10") used to initialize the Jalali datepicker inputs
    public string? SelectedDateFromPersian { get; init; }

    public string? SelectedDateToPersian { get; init; }
}

public sealed class ProductViolationReportViewModel
{
    public Guid Id { get; init; }

    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string? ProductSellerId { get; init; }

    public Guid? ProductOfferId { get; init; }

    public string? SellerId { get; init; }

    public string? SellerName { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string ReporterId { get; init; } = string.Empty;

    public string ReporterPhone { get; init; } = string.Empty;

    public bool IsReviewed { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ReviewedAt { get; init; }

    public string? ReviewedById { get; init; }
}

