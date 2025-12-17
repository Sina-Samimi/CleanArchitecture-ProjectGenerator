using System;
using System.Linq;
using System.Threading.Tasks;
using Attar.Application.Commands.Admin.DeploymentProfiles;
using Attar.Application.Queries.Admin.DeploymentProfiles;
using Attar.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class DeploymentProfilesController : Controller
{
    private readonly IMediator _mediator;

    public DeploymentProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetDeploymentProfilesQuery(), cancellationToken);

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Error))
        {
            TempData["Error"] = result.Error;
        }

        var profiles = result.IsSuccess && result.Value is not null
            ? result.Value.Select(DeploymentProfileListItemViewModel.FromDto).ToArray()
            : Array.Empty<DeploymentProfileListItemViewModel>();

        var model = new DeploymentProfileIndexViewModel
        {
            Profiles = profiles
        };

        SetPageMetadata();
        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new DeploymentProfileFormViewModel();
        return PartialView("_DeploymentProfileModal", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه پروفایل معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetDeploymentProfileByIdQuery(id), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(result.Error)
                ? "پروفایل مورد نظر یافت نشد."
                : result.Error;
            return RedirectToAction(nameof(Index));
        }

        var model = DeploymentProfileFormViewModel.FromDto(result.Value);
        return PartialView("_DeploymentProfileModal", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(DeploymentProfileFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return PartialView("_DeploymentProfileModal", model);
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = model.ToCommand();
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "در ذخیره‌سازی پروفایل خطایی رخ داد.");
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return PartialView("_DeploymentProfileModal", model);
        }

        TempData["Success"] = model.Id.HasValue
            ? "پروفایل با موفقیت به‌روزرسانی شد."
            : "پروفایل جدید با موفقیت ثبت شد.";

        return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه پروفایل معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new DeleteDeploymentProfileCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "در حذف پروفایل خطایی رخ داد.";
        }
        else
        {
            TempData["Success"] = "پروفایل پابلیش با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    private void SetPageMetadata()
    {
        ViewData["Title"] = "پروفایل‌های پابلیش";
        ViewData["Subtitle"] = "مدیریت اطلاعات استقرار خودکار بر روی سرور";
    }
}
