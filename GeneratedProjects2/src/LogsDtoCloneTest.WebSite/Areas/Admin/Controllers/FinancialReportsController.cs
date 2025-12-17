using System;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Queries.Financial;
using LogsDtoCloneTest.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class FinancialReportsController : Controller
{
    private readonly IMediator _mediator;

    public FinancialReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTimeOffset? fromDate, DateTimeOffset? toDate)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var query = new GetFinancialReportsQuery(fromDate, toDate);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Alert.Message"] = result.Error ?? "بارگذاری گزارشات مالی با خطا مواجه شد.";
            TempData["Alert.Type"] = "danger";
        }

        var viewModel = result.IsSuccess && result.Value is not null
            ? FinancialReportsViewModel.FromDto(result.Value)
            : FinancialReportsViewModel.Empty();

        ConfigurePageMetadata();
        return View(viewModel);
    }

    private void ConfigurePageMetadata()
    {
        ViewData["Title"] = "گزارشات مالی";
        ViewData["PageTitle"] = "گزارشات مالی و حسابداری";
        ViewData["PageSubtitle"] = "آمار دقیق فروش، تراکنش‌ها و درآمدها";
    }
}

