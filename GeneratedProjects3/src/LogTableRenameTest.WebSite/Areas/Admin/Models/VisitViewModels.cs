using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LogTableRenameTest.Application.DTOs.Visits;

namespace LogTableRenameTest.WebSite.Areas.Admin.Models;

public sealed class VisitStatisticsViewModel
{
    public VisitStatisticsDto Statistics { get; init; } = new(0, 0, 0, null, null);

    public IReadOnlyCollection<DailyVisitDto> DailyVisits { get; init; } = Array.Empty<DailyVisitDto>();

    public IReadOnlyCollection<PageVisitSummaryDto> PageSummaries { get; init; } = Array.Empty<PageVisitSummaryDto>();

    public VisitFilterViewModel Filter { get; init; } = new();

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class VisitFilterViewModel
{
    [Display(Name = "از تاریخ")]
    public string? FromDatePersian { get; set; }

    [Display(Name = "تا تاریخ")]
    public string? ToDatePersian { get; set; }

    [Display(Name = "صفحه")]
    public Guid? PageId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

}

