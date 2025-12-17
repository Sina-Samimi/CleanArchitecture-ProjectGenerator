using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Commands.Billing;
using Attar.Application.Queries.Billing;
using Attar.Domain.Enums;
using Attar.WebSite.Areas.User.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.Areas.User.Controllers;

[Area("User")]
[Authorize]
public sealed class WithdrawalRequestsController : Controller
{
    private readonly IMediator _mediator;

    public WithdrawalRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "درخواست‌های برداشت از کیف پول";
        ViewData["Sidebar:ActiveTab"] = "wallet";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index", "Wallet");
        }

        var cancellationToken = HttpContext.RequestAborted;

        // Get wallet balance
        var walletQuery = new GetWalletDashboardQuery(userId);
        var walletResult = await _mediator.Send(walletQuery, cancellationToken);
        
        var walletBalance = walletResult.IsSuccess && walletResult.Value is not null
            ? walletResult.Value.Summary.Balance
            : 0m;
        var currency = walletResult.IsSuccess && walletResult.Value is not null
            ? walletResult.Value.Summary.Currency
            : "IRT";

        // Get withdrawal requests (only wallet type)
        var query = new GetWithdrawalRequestsQuery(null, userId, WithdrawalRequestType.Wallet, null, null, null);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت درخواست‌های برداشت.";
            return View(new WalletWithdrawalRequestsViewModel
            {
                WalletBalance = walletBalance,
                Currency = currency,
                GeneratedAt = DateTimeOffset.UtcNow
            });
        }

        // Get wallet withdrawal requests from paginated result
        var walletRequests = result.Value.Items;

        var viewModel = new WalletWithdrawalRequestsViewModel
        {
            Requests = walletRequests
                .Select(r => new WalletWithdrawalRequestListItemViewModel
                {
                    Id = r.Id,
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
                    ProcessedAt = r.ProcessedAt,
                    CreateDate = r.CreateDate,
                    UpdateDate = r.UpdateDate
                })
                .ToList(),
            WalletBalance = walletBalance,
            Currency = currency,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "ثبت درخواست برداشت از کیف پول";
        ViewData["Sidebar:ActiveTab"] = "wallet";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index");
        }

        var cancellationToken = HttpContext.RequestAborted;

        // Get wallet balance
        var walletQuery = new GetWalletDashboardQuery(userId);
        var walletResult = await _mediator.Send(walletQuery, cancellationToken);
        
        var walletBalance = walletResult.IsSuccess && walletResult.Value is not null
            ? walletResult.Value.Summary.Balance
            : 0m;
        var currency = walletResult.IsSuccess && walletResult.Value is not null
            ? walletResult.Value.Summary.Currency
            : "IRT";

        var viewModel = new CreateWalletWithdrawalRequestViewModel
        {
            WalletBalance = walletBalance,
            Currency = currency
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWalletWithdrawalRequestViewModel model)
    {
        ViewData["Title"] = "ثبت درخواست برداشت از کیف پول";
        ViewData["Sidebar:ActiveTab"] = "wallet";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index");
        }

        if (!ModelState.IsValid)
        {
            // Reload wallet balance
            var walletQuery = new GetWalletDashboardQuery(userId);
            var walletResult = await _mediator.Send(walletQuery, HttpContext.RequestAborted);
            if (walletResult.IsSuccess && walletResult.Value is not null)
            {
                model.WalletBalance = walletResult.Value.Summary.Balance;
                model.Currency = walletResult.Value.Summary.Currency;
            }

            return View(model);
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = new CreateWalletWithdrawalRequestCommand(
            userId,
            model.Amount,
            model.Currency,
            model.BankAccountNumber,
            model.CardNumber,
            model.Iban,
            model.BankName,
            model.AccountHolderName,
            model.Description);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در ثبت درخواست برداشت.");

            // Reload wallet balance
            var walletQuery = new GetWalletDashboardQuery(userId);
            var walletResult = await _mediator.Send(walletQuery, cancellationToken);
            if (walletResult.IsSuccess && walletResult.Value is not null)
            {
                model.WalletBalance = walletResult.Value.Summary.Balance;
                model.Currency = walletResult.Value.Summary.Currency;
            }

            return View(model);
        }

        TempData["Success"] = "درخواست برداشت با موفقیت ثبت شد.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index");
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = new CancelWithdrawalRequestCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در لغو درخواست برداشت.";
        }
        else
        {
            TempData["Success"] = "درخواست برداشت با موفقیت لغو شد.";
        }

        return RedirectToAction("Index");
    }
}

