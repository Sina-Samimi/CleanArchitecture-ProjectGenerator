using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Commands.Admin.PaymentSettings;
using Attar.Application.Queries.Admin.PaymentSettings;
using Attar.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public sealed class PaymentSettingsController : Controller
{
    private readonly IMediator _mediator;

    public PaymentSettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetPaymentSettingsQuery(), cancellationToken);

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Error))
        {
            TempData["Error"] = result.Error;
        }

        var model = result.IsSuccess && result.Value is not null
            ? PaymentSettingsViewModel.FromDto(result.Value)
            : new PaymentSettingsViewModel();

        SetPageMetadata();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PaymentSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            SetPageMetadata();
            return View(model);
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = model.ToCommand();
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "در ذخیره‌سازی تنظیمات پرداخت خطایی رخ داد.");
            SetPageMetadata();
            return View(model);
        }

        TempData["Success"] = "تنظیمات پرداخت با موفقیت ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    private void SetPageMetadata()
    {
        ViewData["Title"] = "تنظیمات درگاه پرداخت";
        ViewData["Subtitle"] = "مدیریت تنظیمات درگاه پرداخت زرین‌پال";
        ViewData["Sidebar:ActiveTab"] = "settings";
    }
}
