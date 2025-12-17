using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Commands.Billing;
using LogTableRenameTest.Application.Queries.Billing;
using LogTableRenameTest.Application.Queries.Identity.GetUserLookups;
using LogTableRenameTest.Application.Queries.Identity.GetUsersByIds;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogTableRenameTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class WithdrawalRequestsController : Controller
{
    private readonly IMediator _mediator;

    public WithdrawalRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        WithdrawalRequestStatus? status,
        int page = 1,
        int pageSize = 20)
    {
        ViewData["Title"] = "درخواست‌های برداشت";
        ViewData["Subtitle"] = "مدیریت و پردازش درخواست‌های برداشت";

        var cancellationToken = HttpContext.RequestAborted;
        var pageNumber = page < 1 ? 1 : page;
        var query = new GetWithdrawalRequestsQuery(null, null, null, status, pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت درخواست‌های برداشت.";
            return View(new WithdrawalRequestsViewModel
            {
                FilterStatus = status,
                PageNumber = pageNumber,
                PageSize = pageSize,
                GeneratedAt = DateTimeOffset.UtcNow
            });
        }

        var data = result.Value;

        // Collect all user IDs (sellers and users)
        var userIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in data.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.SellerId))
            {
                userIds.Add(item.SellerId);
            }
            if (!string.IsNullOrWhiteSpace(item.UserId))
            {
                userIds.Add(item.UserId);
            }
        }

        // Get user lookups with phone numbers
        var userLookupsDict = new Dictionary<string, (string DisplayName, string? PhoneNumber)>(StringComparer.Ordinal);
        if (userIds.Count > 0)
        {
            var userLookupsResult = await _mediator.Send(new GetUsersByIdsQuery(userIds.ToList()), cancellationToken);
            if (userLookupsResult.IsSuccess && userLookupsResult.Value is not null)
            {
                foreach (var (userId, userLookup) in userLookupsResult.Value)
                {
                    userLookupsDict[userId] = (userLookup.DisplayName, userLookup.PhoneNumber);
                }
            }
        }

        var viewModel = new WithdrawalRequestsViewModel
        {
            Requests = data.Items
                .Select(r => new WithdrawalRequestListItemViewModel
                {
                    Id = r.Id,
                    RequestType = r.RequestType,
                    SellerId = r.SellerId,
                    SellerName = !string.IsNullOrWhiteSpace(r.SellerId) && userLookupsDict.TryGetValue(r.SellerId, out var sellerInfo)
                        ? sellerInfo.DisplayName
                        : null,
                    SellerPhoneNumber = !string.IsNullOrWhiteSpace(r.SellerId) && userLookupsDict.TryGetValue(r.SellerId, out var sellerInfo2)
                        ? sellerInfo2.PhoneNumber
                        : null,
                    UserId = r.UserId,
                    UserName = !string.IsNullOrWhiteSpace(r.UserId) && userLookupsDict.TryGetValue(r.UserId, out var userInfo)
                        ? userInfo.DisplayName
                        : null,
                    UserPhoneNumber = !string.IsNullOrWhiteSpace(r.UserId) && userLookupsDict.TryGetValue(r.UserId, out var userInfo2)
                        ? userInfo2.PhoneNumber
                        : null,
                    Amount = r.Amount,
                    Currency = r.Currency,
                    BankAccountNumber = r.BankAccountNumber,
                    CardNumber = r.CardNumber,
                    Iban = r.Iban,
                    BankName = r.BankName,
                    AccountHolderName = r.AccountHolderName,
                    Description = r.Description,
                    AdminNotes = r.AdminNotes,
                    Status = r.Status,
                    ProcessedByUserId = r.ProcessedByUserId,
                    ProcessedAt = r.ProcessedAt,
                    CreateDate = r.CreateDate,
                    UpdateDate = r.UpdateDate
                })
                .ToList(),
            FilterStatus = status,
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            GeneratedAt = data.GeneratedAt
        };

        ViewData["StatusOptions"] = BuildStatusOptions(status);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        ViewData["Title"] = "جزئیات درخواست برداشت";
        ViewData["Subtitle"] = "مشاهده و مدیریت درخواست برداشت";

        var cancellationToken = HttpContext.RequestAborted;
        var query = new GetWithdrawalRequestDetailsQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "درخواست برداشت یافت نشد.";
            return RedirectToAction("Index");
        }

        var details = result.Value;

        // Get user lookups
        var userLookupsResult = await _mediator.Send(new GetUserLookupsQuery(), cancellationToken);
        var userLookups = userLookupsResult.IsSuccess && userLookupsResult.Value is not null
            ? userLookupsResult.Value.ToDictionary(u => u.Id, u => u.DisplayName)
            : new System.Collections.Generic.Dictionary<string, string>();

        var viewModel = new WithdrawalRequestDetailsViewModel
        {
            Id = details.Id,
            RequestType = details.RequestType,
            SellerId = details.SellerId,
            SellerName = !string.IsNullOrWhiteSpace(details.SellerId) ? userLookups.GetValueOrDefault(details.SellerId) : null,
            UserId = details.UserId,
            UserName = !string.IsNullOrWhiteSpace(details.UserId) ? userLookups.GetValueOrDefault(details.UserId) : null,
            Amount = details.Amount,
            Currency = details.Currency,
            BankAccountNumber = details.BankAccountNumber,
            CardNumber = details.CardNumber,
            Iban = details.Iban,
            BankName = details.BankName,
            AccountHolderName = details.AccountHolderName,
            Description = details.Description,
            AdminNotes = details.AdminNotes,
            Status = details.Status,
            ProcessedByUserId = details.ProcessedByUserId,
            ProcessedAt = details.ProcessedAt,
            WalletTransactionId = details.WalletTransactionId,
            CreateDate = details.CreateDate,
            UpdateDate = details.UpdateDate
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, string? adminNotes)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var command = new ApproveWithdrawalRequestCommand(id, adminNotes);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در تایید درخواست برداشت.";
        }
        else
        {
            TempData["Success"] = "درخواست برداشت با موفقیت تایید شد.";
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string? adminNotes)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var command = new RejectWithdrawalRequestCommand(id, adminNotes);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در رد درخواست برداشت.";
        }
        else
        {
            TempData["Success"] = "درخواست برداشت رد شد.";
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Details", new { id });
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = new ProcessWithdrawalRequestCommand(id, userId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در پردازش درخواست برداشت.";
        }
        else
        {
            TempData["Success"] = "درخواست برداشت با موفقیت پردازش شد و مبلغ از کیف پول فروشنده کسر شد.";
        }

        return RedirectToAction("Details", new { id });
    }

    private static IReadOnlyCollection<SelectListItem> BuildStatusOptions(WithdrawalRequestStatus? selected)
    {
        return Enum.GetValues(typeof(WithdrawalRequestStatus))
            .Cast<WithdrawalRequestStatus>()
            .Select(status => new SelectListItem
            {
                Value = status.ToString(),
                Text = status switch
                {
                    WithdrawalRequestStatus.Pending => "در انتظار بررسی",
                    WithdrawalRequestStatus.Approved => "تایید شده",
                    WithdrawalRequestStatus.Processed => "پرداخت شده",
                    WithdrawalRequestStatus.Rejected => "رد شده",
                    WithdrawalRequestStatus.Cancelled => "لغو شده",
                    _ => status.ToString()
                },
                Selected = status == selected
            })
            .ToList();
    }
}

