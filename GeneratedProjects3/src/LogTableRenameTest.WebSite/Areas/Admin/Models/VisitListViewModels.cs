using System;
using System.Collections.Generic;
using LogTableRenameTest.Application.DTOs.Visits;

namespace LogTableRenameTest.WebSite.Areas.Admin.Models;

public sealed class VisitListViewModel
{
    public IReadOnlyCollection<SiteVisitListItemDto> Visits { get; init; } = Array.Empty<SiteVisitListItemDto>();

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public string? FilterFromDate { get; init; }

    public string? FilterToDate { get; init; }

    public string? FilterIpAddress { get; init; }

    public string? FilterDeviceType { get; init; }

    public string? FilterBrowser { get; init; }

    public string? FilterOperatingSystem { get; init; }

    public VisitStatisticsSummaryDto StatisticsSummary { get; init; } = new(
        Array.Empty<DeviceTypeStatDto>(),
        Array.Empty<OperatingSystemStatDto>(),
        Array.Empty<BrowserStatDto>());
}

