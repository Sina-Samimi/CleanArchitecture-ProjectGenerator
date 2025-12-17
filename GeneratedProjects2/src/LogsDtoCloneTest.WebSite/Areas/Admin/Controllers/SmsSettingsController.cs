using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Commands.Admin.SmsSettings;
using LogsDtoCloneTest.Application.Queries.Admin.SmsSettings;
using LogsDtoCloneTest.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public sealed class SmsSettingsController : Controller
{
    private readonly IMediator _mediator;

    public SmsSettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetSmsSettingsQuery(), cancellationToken);

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Error))
        {
            TempData["Error"] = result.Error;
        }

        var model = result.IsSuccess && result.Value is not null
            ? SmsSettingsViewModel.FromDto(result.Value)
            : new SmsSettingsViewModel();

        SetPageMetadata();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SmsSettingsViewModel model)
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
            ModelState.AddModelError(string.Empty, result.Error ?? "در ذخیره‌سازی تنظیمات پیامک خطایی رخ داد.");
            SetPageMetadata();
            return View(model);
        }

        TempData["Success"] = "تنظیمات پیامک با موفقیت ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    private void SetPageMetadata()
    {
        ViewData["Title"] = "تنظیمات پنل پیامکی";
        ViewData["Subtitle"] = "مدیریت تنظیمات سرویس پیامک";
        ViewData["Sidebar:ActiveTab"] = "settings";
    }
}
