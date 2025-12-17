using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Commands.Billing.Wallet;
using LogTableRenameTest.Application.Queries.Identity.GetUserLookups;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.WebSite.Areas.Admin.Models;
using LogTableRenameTest.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogTableRenameTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class WalletsController : Controller
{
    private readonly IMediator _mediator;

    public WalletsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Charge()
    {
        var model = new WalletChargeFormViewModel
        {
            PaymentMethodOptions = BuildPaymentMethodOptions()
        };

        await PopulateUserOptionsAsync(model, HttpContext.RequestAborted);
        PrepareMetadata();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Charge(WalletChargeFormViewModel model)
    {
        var cancellationToken = HttpContext.RequestAborted;
        await PopulateUserOptionsAsync(model, cancellationToken);
        model.PaymentMethodOptions = BuildPaymentMethodOptions(model.PaymentMethod);
        PrepareMetadata();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new AdminChargeWalletCommand(
            model.UserId!,
            model.Amount,
            model.Currency,
            model.InvoiceTitle,
            model.InvoiceDescription,
            model.TransactionDescription,
            model.PaymentReference,
            model.PaymentMethod,
            IssueDate: null,
            TransactionOccurredAt: null);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "در ثبت شارژ کیف پول خطایی رخ داد.");
            return View(model);
        }

        var chargeResult = result.Value;
        TempData["Success"] = $"شارژ کیف پول با موفقیت ثبت شد. شماره فاکتور: {chargeResult.InvoiceNumber}";
        return RedirectToAction("Details", "Invoices", new { area = "Admin", id = chargeResult.InvoiceId });
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Duration = 0)]
    public async Task<IActionResult> SearchUsers([FromQuery] string? term, CancellationToken cancellationToken)
    {
        var query = new SearchUserLookupsQuery(term, MaxResults: 50);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return Json(new { results = Array.Empty<object>() });
        }

        var users = result.Value.Select(user =>
        {
            var text = user.DisplayName;
            if (!string.IsNullOrWhiteSpace(user.Email) &&
                !string.Equals(user.Email, user.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                text = $"{user.DisplayName} ({user.Email})";
            }

            if (!user.IsActive)
            {
                text = $"{text} (غیرفعال)";
            }

            return new
            {
                id = user.Id,
                text = text
            };
        });

        return Json(new { results = users });
    }

    private void PrepareMetadata()
    {
        ViewData["Title"] = "شارژ کیف پول کاربران";
        ViewData["Subtitle"] = "افزودن اعتبار به کیف پول کاربران همراه با ثبت فاکتور و تراکنش";
    }

    private async Task PopulateUserOptionsAsync(WalletChargeFormViewModel model, CancellationToken cancellationToken)
    {
        if (model is null)
        {
            return;
        }

        var result = await _mediator.Send(new GetUserLookupsQuery(), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            model.UserOptions = Array.Empty<SelectListItem>();
            return;
        }

        var options = new List<SelectListItem>(result.Value.Count);

        foreach (var userLookup in result.Value)
        {
            var text = userLookup.DisplayName;

            if (!string.IsNullOrWhiteSpace(userLookup.Email) &&
                !string.Equals(userLookup.Email, userLookup.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                text = $"{userLookup.DisplayName} ({userLookup.Email})";
            }

            if (!userLookup.IsActive)
            {
                text = $"{text} (غیرفعال)";
            }

            options.Add(new SelectListItem
            {
                Value = userLookup.Id,
                Text = text,
                Selected = !string.IsNullOrWhiteSpace(model.UserId) &&
                          string.Equals(userLookup.Id, model.UserId, StringComparison.Ordinal)
            });
        }

        if (!string.IsNullOrWhiteSpace(model.UserId) &&
            options.All(option => option.Value != model.UserId))
        {
            options.Insert(0, new SelectListItem
            {
                Value = model.UserId,
                Text = $"{model.UserId} (کاربر یافت نشد یا غیرفعال است)",
                Selected = true
            });
        }

        model.UserOptions = options;
    }

    private static IReadOnlyCollection<SelectListItem> BuildPaymentMethodOptions(PaymentMethod selected = PaymentMethod.Cash)
    {
        return Enum.GetValues(typeof(PaymentMethod))
            .Cast<PaymentMethod>()
            .Select(method => new SelectListItem
            {
                Value = method.ToString(),
                Text = method.GetDisplayName(),
                Selected = method == selected
            })
            .ToList();
    }
}
