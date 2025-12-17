using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.DTOs.Billing;
using LogTableRenameTest.Application.Queries.Billing;
using LogTableRenameTest.Application.Queries.Orders;
using LogTableRenameTest.Domain.Entities;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.Areas.User.Controllers;

[Area("User")]
[Authorize]
public sealed class InvoiceController : Controller
{
    private readonly IMediator _mediator;
    private readonly UserManager<ApplicationUser> _userManager;

    public InvoiceController(IMediator mediator, UserManager<ApplicationUser> userManager)
    {
        _mediator = mediator;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? searchTerm,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("PhoneLogin", "Account", new { area = "", returnUrl = Url.Action("Index") });
        }

        PrepareLayoutMetadata(user);

        DateTimeOffset? fromDateParsed = UserFilterFormatting.ParsePersianDate(fromDate, toExclusiveEnd: false, out _);
        DateTimeOffset? toDateParsed = UserFilterFormatting.ParsePersianDate(toDate, toExclusiveEnd: true, out _);

        var filter = new InvoiceListFilterDto(
            SearchTerm: searchTerm,
            UserId: user.Id,
            Status: status,
            IssueDateFrom: fromDateParsed,
            IssueDateTo: toDateParsed,
            PageNumber: Math.Max(1, page),
            PageSize: Math.Max(1, Math.Min(100, pageSize))
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
                GeneratedAt: DateTimeOffset.UtcNow,
                PageNumber: page,
                PageSize: pageSize
            ));
        }

        ViewData["Filter"] = new
        {
            SearchTerm = searchTerm,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate
        };

        return View("Index", result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("PhoneLogin", "Account", new { area = "", returnUrl = Url.Action("Details", new { id }) });
        }

        PrepareLayoutMetadata(user);

        var result = await _mediator.Send(new GetUserInvoiceDetailsQuery(id, user.Id), cancellationToken);
        
        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "فاکتور مورد نظر یافت نشد.";
            return RedirectToAction("Index");
        }

        // Load shipment trackings
        var trackingsResult = await _mediator.Send(new GetShipmentTrackingsByInvoiceQuery(id), cancellationToken);
        if (trackingsResult.IsSuccess && trackingsResult.Value is not null)
        {
            ViewData["ShipmentTrackings"] = trackingsResult.Value.Trackings;
        }

        return View("Details", result.Value);
    }

    // Temporarily disabled - commented out for future implementation
    /*
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("PhoneLogin", "Account", new { area = "", returnUrl = Url.Action("Index") });
        }

        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه فاکتور معتبر نیست.";
            return RedirectToAction("Index");
        }

        var ownership = await _mediator.Send(new LogTableRenameTest.Application.Queries.Billing.GetUserInvoiceDetailsQuery(id, user.Id), cancellationToken);
        if (!ownership.IsSuccess || ownership.Value is null)
        {
            TempData["Error"] = ownership.Error ?? "فاکتور مورد نظر یافت نشد یا دسترسی ندارید.";
            return RedirectToAction("Index");
        }

        var result = await _mediator.Send(new LogTableRenameTest.Application.Commands.Orders.CancelOrderCommand(id), cancellationToken);

        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "درخواست لغو سفارش با موفقیت ثبت شد."
            : result.Error ?? "در لغو سفارش خطایی رخ داد.";

        return RedirectToAction("Details", new { id });
    }
    */

    private void PrepareLayoutMetadata(ApplicationUser user)
    {
        var displayName = string.IsNullOrWhiteSpace(user.FullName) ? "کاربر گرامی" : user.FullName.Trim();
        var emailDisplay = string.IsNullOrWhiteSpace(user.Email) ? "ایمیل ثبت نشده" : user.Email;
        var phoneDisplay = string.IsNullOrWhiteSpace(user.PhoneNumber) ? "شماره ثبت نشده" : user.PhoneNumber;

        ViewData["AccountName"] = displayName;
        ViewData["AccountInitial"] = displayName.Length > 0 ? displayName[0].ToString() : "ک";
        ViewData["AccountEmail"] = emailDisplay;
        ViewData["AccountPhone"] = phoneDisplay;
        ViewData["Sidebar:Email"] = emailDisplay;
        ViewData["Sidebar:Phone"] = phoneDisplay;
        ViewData["GreetingSubtitle"] = "مدیریت فاکتورهای شما";
        ViewData["GreetingTitle"] = $"سلام، {displayName} 📄";
        ViewData["AccountAvatarUrl"] = user.AvatarPath;
        ViewData["Sidebar:Completion"] = 100;
    }
}
