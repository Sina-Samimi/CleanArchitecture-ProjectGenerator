using System;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Dashboard;
using Arsis.Application.Queries.Dashboard;
using Arsis.SharedKernel.Extensions;
using EndPoint.WebSite.Areas.Admin.Models.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class DashboardController : Controller
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetSystemPerformanceSummaryQuery(DateTimeOffset.UtcNow), cancellationToken);

        SystemPerformanceSummaryViewModel viewModel;
        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Alert.Message"] = result.Error ?? "بارگذاری داشبورد با خطا مواجه شد.";
            TempData["Alert.Type"] = "danger";
            viewModel = SystemPerformanceSummaryViewModel.FromDto(BuildEmptySummary(DateTimeOffset.UtcNow));
        }
        else
        {
            viewModel = SystemPerformanceSummaryViewModel.FromDto(result.Value);
        }

        ConfigurePageMetadata(viewModel);
        return View(viewModel);
    }

    private static SystemPerformanceSummaryDto BuildEmptySummary(DateTimeOffset referenceTime)
    {
        var currentPeriodEnd = referenceTime;
        var currentPeriodStart = referenceTime.AddDays(-30);
        var previousPeriodStart = referenceTime.AddDays(-60);
        var previousPeriodEnd = currentPeriodStart;
        var today = DateOnly.FromDateTime(referenceTime.Date);
        var currentWeekStart = today.AddDays(-6);
        var previousWeekStart = currentWeekStart.AddDays(-7);

        return new SystemPerformanceSummaryDto(
            new PeriodWindowDto(
                currentPeriodStart,
                currentPeriodEnd,
                previousPeriodStart,
                previousPeriodEnd,
                currentWeekStart,
                previousWeekStart),
            new PeopleMetricsDto(0, 0, 0, 0, 0, 0, 0, 0),
            new CommerceMetricsDto(0m, 0m, 0m, 0m, 0m, 0, 0, 0, 0, 0),
            new LearningMetricsDto(0, 0, 0, 0, 0m, 0m, 0, 0, 0d, 0d),
            new ContentMetricsDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            referenceTime);
    }

    private void ConfigurePageMetadata(SystemPerformanceSummaryViewModel model)
    {
        ViewData["Title"] = "داشبورد مدیریت سیستم";
        ViewData["Subtitle"] = "خلاصه عملکرد کلیدی بخش‌های آرسیس در یک نگاه";
        ViewData["ShowSearch"] = false;
        ViewData["SearchPlaceholder"] = "جستجوی سریع در داشبورد";
        ViewData["GreetingTitle"] = "سلام، خوش آمدید! 👋";
        ViewData["GreetingSubtitle"] = $"آخرین بروزرسانی: {model.GeneratedAt.ToPersianDateString()}";
    }
}
