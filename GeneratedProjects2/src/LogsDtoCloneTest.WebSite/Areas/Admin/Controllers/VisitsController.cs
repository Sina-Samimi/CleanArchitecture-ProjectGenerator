using System;
using System.Globalization;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Queries.Visits;
using LogsDtoCloneTest.WebSite.Areas.Admin.Models;
using LogsDtoCloneTest.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class VisitsController : Controller
{
    private const int DefaultPageSize = 10;
    private const int DefaultChartDays = 30;

    private readonly IMediator _mediator;

    public VisitsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(VisitFilterViewModel? filter)
    {
        filter ??= new VisitFilterViewModel();

        // Parse Persian dates
        var fromDateOffset = UserFilterFormatting.ParsePersianDate(filter.FromDatePersian, toExclusiveEnd: false, out _);
        var toDateOffset = UserFilterFormatting.ParsePersianDate(filter.ToDatePersian, toExclusiveEnd: true, out _);

        // Convert DateTimeOffset? to DateOnly?
        DateOnly? fromDate = fromDateOffset.HasValue ? DateOnly.FromDateTime(fromDateOffset.Value.Date) : null;
        DateOnly? toDate = toDateOffset.HasValue ? DateOnly.FromDateTime(toDateOffset.Value.Date) : null;

        // For chart, get last 30 days if no date filter is specified
        DateOnly? chartFromDate = fromDate;
        if (!fromDate.HasValue && !toDate.HasValue)
        {
            chartFromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-DefaultChartDays));
        }

        var pageNumber = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 && filter.PageSize <= 100 ? filter.PageSize : DefaultPageSize;

        var cancellationToken = HttpContext.RequestAborted;

        // Get site visit statistics
        var siteStatsQuery = new GetVisitStatisticsQuery(
            IsPageVisit: false,
            PageId: null,
            FromDate: fromDate,
            ToDate: toDate);

        var siteStatsResult = await _mediator.Send(siteStatsQuery, cancellationToken);
        var siteStatistics = siteStatsResult.IsSuccess ? siteStatsResult.Value! : new Application.DTOs.Visits.VisitStatisticsDto(0, 0, 0, null, null);

        // Get daily site visits for chart (last 30 days if no filter)
        var dailySiteVisitsQuery = new GetDailyVisitsQuery(
            IsPageVisit: false,
            PageId: null,
            FromDate: chartFromDate,
            ToDate: toDate);

        var dailySiteVisitsResult = await _mediator.Send(dailySiteVisitsQuery, cancellationToken);
        var dailySiteVisits = dailySiteVisitsResult.IsSuccess ? dailySiteVisitsResult.Value! : Array.Empty<Application.DTOs.Visits.DailyVisitDto>();

        // Get page visit summaries with pagination
        var pageSummariesQuery = new GetPageVisitSummariesQuery(
            FromDate: fromDate,
            ToDate: toDate,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var pageSummariesResult = await _mediator.Send(pageSummariesQuery, cancellationToken);
        var pageSummaries = pageSummariesResult.IsSuccess ? pageSummariesResult.Value! : Array.Empty<Application.DTOs.Visits.PageVisitSummaryDto>();

        // Get total count for pagination
        var countQuery = new GetPageVisitSummariesCountQuery(
            FromDate: fromDate,
            ToDate: toDate);

        var countResult = await _mediator.Send(countQuery, cancellationToken);
        var totalCount = countResult.IsSuccess ? countResult.Value : 0;

        var viewModel = new VisitStatisticsViewModel
        {
            Statistics = siteStatistics,
            DailyVisits = dailySiteVisits,
            PageSummaries = pageSummaries,
            Filter = filter,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        ViewData["Title"] = "گزارشات بازدید";
        ViewData["Subtitle"] = "آمار و گزارشات بازدید سایت و صفحات";

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20, string? fromDate = null, string? toDate = null, string? ipAddress = null, string? deviceType = null, string? browser = null, string? operatingSystem = null)
    {
        ViewData["Title"] = "لیست بازدیدها";
        ViewData["Subtitle"] = "نمایش و مدیریت لیست بازدیدهای سایت";

        // Parse Persian dates
        DateOnly? fromDateOnly = null;
        DateOnly? toDateOnly = null;

        if (!string.IsNullOrWhiteSpace(fromDate))
        {
            var fromDateOffset = UserFilterFormatting.ParsePersianDate(fromDate, toExclusiveEnd: false, out _);
            if (fromDateOffset.HasValue)
            {
                fromDateOnly = DateOnly.FromDateTime(fromDateOffset.Value.Date);
            }
        }

        if (!string.IsNullOrWhiteSpace(toDate))
        {
            var toDateOffset = UserFilterFormatting.ParsePersianDate(toDate, toExclusiveEnd: true, out _);
            if (toDateOffset.HasValue)
            {
                toDateOnly = DateOnly.FromDateTime(toDateOffset.Value.Date);
            }
        }

        var query = new GetSiteVisitsQuery(
            FromDate: fromDateOnly,
            ToDate: toDateOnly,
            IpAddress: ipAddress,
            DeviceType: deviceType,
            Browser: browser,
            OperatingSystem: operatingSystem,
            PageNumber: page,
            PageSize: pageSize);

        var result = await _mediator.Send(query, HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت لیست بازدیدها.";
            return RedirectToAction(nameof(Index));
        }

        // Get statistics summary for charts
        var summaryQuery = new GetVisitStatisticsSummaryQuery(fromDateOnly, toDateOnly);
        var summaryResult = await _mediator.Send(summaryQuery, HttpContext.RequestAborted);
        var statisticsSummary = summaryResult.IsSuccess && summaryResult.Value is not null
            ? summaryResult.Value
            : new Application.DTOs.Visits.VisitStatisticsSummaryDto(
                Array.Empty<Application.DTOs.Visits.DeviceTypeStatDto>(),
                Array.Empty<Application.DTOs.Visits.OperatingSystemStatDto>(),
                Array.Empty<Application.DTOs.Visits.BrowserStatDto>());

        var viewModel = new VisitListViewModel
        {
            Visits = result.Value.Items,
            PageNumber = result.Value.PageNumber,
            PageSize = result.Value.PageSize,
            TotalCount = result.Value.TotalCount,
            TotalPages = result.Value.TotalPages,
            FilterFromDate = fromDate,
            FilterToDate = toDate,
            FilterIpAddress = ipAddress,
            FilterDeviceType = deviceType,
            FilterBrowser = browser,
            FilterOperatingSystem = operatingSystem,
            StatisticsSummary = statisticsSummary
        };

        return View(viewModel);
    }
}

