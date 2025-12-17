using System.Threading.Tasks;
using LogTableRenameTest.Application.Queries.Admin.FinancialSettings;
using LogTableRenameTest.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class FinancialSettingsController : Controller
{
    private readonly IMediator _mediator;

    public FinancialSettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetFinancialSettingsQuery(), cancellationToken);

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Error))
        {
            TempData["Error"] = result.Error;
        }

        var model = result.IsSuccess && result.Value is not null
            ? FinancialSettingsViewModel.FromDto(result.Value)
            : new FinancialSettingsViewModel();

        SetPageMetadata();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(FinancialSettingsViewModel model)
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
            ModelState.AddModelError(string.Empty, result.Error ?? "در ذخیره‌سازی تنظیمات مالی خطایی رخ داد.");
            SetPageMetadata();
            return View(model);
        }

        TempData["Success"] = "تنظیمات مالی با موفقیت ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    private void SetPageMetadata()
    {
        ViewData["Title"] = "تنظیمات مالی";
        ViewData["Subtitle"] = "تعریف سهم فروشنده، مالیات و کارمزدهای فروش";
    }
}
