using System.Threading.Tasks;
using Arsis.Application.Commands.Admin.SiteSettings;
using Arsis.Application.Queries.Admin.SiteSettings;
using EndPoint.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class SiteSettingsController : Controller
{
    private readonly IMediator _mediator;

    public SiteSettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetSiteSettingsQuery(), cancellationToken);

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Error))
        {
            TempData["Error"] = result.Error;
        }

        var model = result.IsSuccess && result.Value is not null
            ? SiteSettingsViewModel.FromDto(result.Value)
            : new SiteSettingsViewModel();

        SetPageMetadata();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteSettingsViewModel model)
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
            ModelState.AddModelError(string.Empty, result.Error ?? "در ذخیره‌سازی تنظیمات سایت خطایی رخ داد.");
            SetPageMetadata();
            return View(model);
        }

        TempData["Success"] = "تنظیمات سایت با موفقیت ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    private void SetPageMetadata()
    {
        ViewData["Title"] = "تنظیمات سایت";
        ViewData["Subtitle"] = "مدیریت اطلاعات تماس و برند پلتفرم";
    }
}
