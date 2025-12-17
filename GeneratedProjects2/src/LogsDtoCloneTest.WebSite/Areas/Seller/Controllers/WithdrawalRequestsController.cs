using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Commands.Billing;
using LogsDtoCloneTest.Application.Queries.Billing;
using LogsDtoCloneTest.Application.Queries.Sellers;
using LogsDtoCloneTest.SharedKernel.Authorization;
using LogsDtoCloneTest.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
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
        ViewData["Title"] = "درخواست‌های برداشت";
        ViewData["Sidebar:ActiveTab"] = "withdrawals";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index", "Dashboard");
        }

        var cancellationToken = HttpContext.RequestAborted;

        // Get total revenue and total withdrawals
        var totalRevenueQuery = new GetSellerPaymentsQuery(userId);
        var totalRevenueResult = await _mediator.Send(totalRevenueQuery, cancellationToken);
        
        var totalRevenue = totalRevenueResult.IsSuccess && totalRevenueResult.Value is not null
            ? totalRevenueResult.Value.TotalRevenue
            : 0m;
        var currency = "IRT"; // Default currency

        var totalWithdrawalsQuery = new GetSellerTotalWithdrawalsQuery(userId);
        var totalWithdrawalsResult = await _mediator.Send(totalWithdrawalsQuery, cancellationToken);
        
        var totalWithdrawn = totalWithdrawalsResult.IsSuccess
            ? totalWithdrawalsResult.Value
            : 0m;
        
        var availableAmount = totalRevenue - totalWithdrawn;

        // Get withdrawal requests (only seller revenue type)
        var query = new GetWithdrawalRequestsQuery(userId, null, Domain.Enums.WithdrawalRequestType.SellerRevenue, null, null, null);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت درخواست‌های برداشت.";
            return View(new WithdrawalRequestsViewModel
            {
                TotalRevenue = totalRevenue,
                TotalWithdrawn = totalWithdrawn,
                AvailableAmount = availableAmount,
                Currency = currency,
                GeneratedAt = DateTimeOffset.UtcNow
            });
        }

        // Get seller revenue withdrawal requests from paginated result
        var sellerRevenueRequests = result.Value.Items;

        var viewModel = new WithdrawalRequestsViewModel
        {
            Requests = sellerRevenueRequests
                .Select(r => new WithdrawalRequestListItemViewModel
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
            TotalRevenue = totalRevenue,
            TotalWithdrawn = totalWithdrawn,
            AvailableAmount = availableAmount,
            Currency = currency,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "ثبت درخواست برداشت";
        ViewData["Sidebar:ActiveTab"] = "withdrawals";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index");
        }

        var cancellationToken = HttpContext.RequestAborted;

        // Get total revenue and total withdrawals
        var totalRevenueQuery = new GetSellerPaymentsQuery(userId);
        var totalRevenueResult = await _mediator.Send(totalRevenueQuery, cancellationToken);
        
        var totalRevenue = totalRevenueResult.IsSuccess && totalRevenueResult.Value is not null
            ? totalRevenueResult.Value.TotalRevenue
            : 0m;
        var currency = "IRT";

        var totalWithdrawalsQuery = new GetSellerTotalWithdrawalsQuery(userId);
        var totalWithdrawalsResult = await _mediator.Send(totalWithdrawalsQuery, cancellationToken);
        
        var totalWithdrawn = totalWithdrawalsResult.IsSuccess
            ? totalWithdrawalsResult.Value
            : 0m;
        
        var availableAmount = totalRevenue - totalWithdrawn;

        var viewModel = new CreateWithdrawalRequestViewModel
        {
            TotalRevenue = totalRevenue,
            TotalWithdrawn = totalWithdrawn,
            AvailableAmount = availableAmount,
            Currency = currency
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWithdrawalRequestViewModel model)
    {
        ViewData["Title"] = "ثبت درخواست برداشت";
        ViewData["Sidebar:ActiveTab"] = "withdrawals";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            TempData["Error"] = "کاربر احراز هویت نشده است.";
            return RedirectToAction("Index");
        }

        if (!ModelState.IsValid)
        {
            // Reload total revenue and withdrawals
            var totalRevenueQuery = new GetSellerPaymentsQuery(userId);
            var totalRevenueResult = await _mediator.Send(totalRevenueQuery, HttpContext.RequestAborted);
            var totalRevenue = totalRevenueResult.IsSuccess && totalRevenueResult.Value is not null
                ? totalRevenueResult.Value.TotalRevenue
                : 0m;

            var totalWithdrawalsQuery = new GetSellerTotalWithdrawalsQuery(userId);
            var totalWithdrawalsResult = await _mediator.Send(totalWithdrawalsQuery, HttpContext.RequestAborted);
            var totalWithdrawn = totalWithdrawalsResult.IsSuccess
                ? totalWithdrawalsResult.Value
                : 0m;

            model.TotalRevenue = totalRevenue;
            model.TotalWithdrawn = totalWithdrawn;
            model.AvailableAmount = totalRevenue - totalWithdrawn;

            return View(model);
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = new CreateSellerRevenueWithdrawalRequestCommand(
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

            // Reload total revenue and withdrawals
            var totalRevenueQuery = new GetSellerPaymentsQuery(userId);
            var totalRevenueResult = await _mediator.Send(totalRevenueQuery, cancellationToken);
            var totalRevenue = totalRevenueResult.IsSuccess && totalRevenueResult.Value is not null
                ? totalRevenueResult.Value.TotalRevenue
                : 0m;

            var totalWithdrawalsQuery = new GetSellerTotalWithdrawalsQuery(userId);
            var totalWithdrawalsResult = await _mediator.Send(totalWithdrawalsQuery, cancellationToken);
            var totalWithdrawn = totalWithdrawalsResult.IsSuccess
                ? totalWithdrawalsResult.Value
                : 0m;

            model.TotalRevenue = totalRevenue;
            model.TotalWithdrawn = totalWithdrawn;
            model.AvailableAmount = totalRevenue - totalWithdrawn;

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

