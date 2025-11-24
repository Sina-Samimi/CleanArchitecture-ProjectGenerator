using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Billing;
using Arsis.Application.Queries.Billing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Areas.User.Controllers;

[Area("User")]
[Authorize]
public sealed class InvoiceController : Controller
{
    private readonly IMediator _mediator;

    public InvoiceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("PhoneLogin", "Account", new { area = "", returnUrl = Url.Action("Index") });
        }

        var filter = new InvoiceListFilterDto(
            SearchTerm: null,
            UserId: userId,
            Status: null,
            IssueDateFrom: null,
            IssueDateTo: null
        );

        var result = await _mediator.Send(new GetInvoiceListQuery(filter), cancellationToken);
        
        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت لیست فاکتورها.";
            return View("Index", new InvoiceListResultDto(
                Items: Array.Empty<InvoiceListItemDto>(),
                Summary: new InvoiceSummaryMetricsDto(
                    TotalInvoices: 0,
                    DraftInvoices: 0,
                    PendingInvoices: 0,
                    PaidInvoices: 0,
                    PartiallyPaidInvoices: 0,
                    CancelledInvoices: 0,
                    OverdueInvoices: 0,
                    TotalBilledAmount: 0,
                    TotalOutstandingAmount: 0,
                    TotalCollectedAmount: 0
                ),
                GeneratedAt: DateTimeOffset.UtcNow
            ));
        }

        return View("Index", result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("PhoneLogin", "Account", new { area = "", returnUrl = Url.Action("Details", new { id }) });
        }

        var result = await _mediator.Send(new GetUserInvoiceDetailsQuery(id, userId), cancellationToken);
        
        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "فاکتور مورد نظر یافت نشد.";
            return RedirectToAction("Index");
        }

        return View("Details", result.Value);
    }

    private string? GetUserId()
        => User?.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
}
