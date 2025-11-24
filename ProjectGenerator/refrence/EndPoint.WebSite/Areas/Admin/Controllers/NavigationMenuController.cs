using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arsis.Application.Commands.Admin.NavigationMenu;
using Arsis.Application.Queries.Admin.NavigationMenu;
using EndPoint.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class NavigationMenuController : Controller
{
    private readonly IMediator _mediator;

    public NavigationMenuController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var itemsResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), cancellationToken);

        var items = itemsResult.IsSuccess && itemsResult.Value is not null
            ? itemsResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
            : new List<NavigationMenuItemViewModel>();

        if (!itemsResult.IsSuccess && !string.IsNullOrWhiteSpace(itemsResult.Error))
        {
            TempData["Error"] = itemsResult.Error;
        }

        var form = new NavigationMenuItemFormViewModel();

        if (id.HasValue)
        {
            var itemResult = await _mediator.Send(new GetNavigationMenuItemQuery(id.Value), cancellationToken);

            if (itemResult.IsSuccess && itemResult.Value is not null)
            {
                form = NavigationMenuItemFormViewModel.FromDto(itemResult.Value);
            }
            else if (!string.IsNullOrWhiteSpace(itemResult.Error))
            {
                TempData["Error"] = itemResult.Error;
            }
        }

        var model = new NavigationMenuPageViewModel
        {
            Items = items,
            Form = form,
            ParentOptions = BuildParentOptions(items, form.Id)
        };

        SetPageMetadata();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NavigationMenuItemFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return await ReturnToIndexWithModelAsync(form);
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(form.ToCreateCommand(), cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "ثبت آیتم منو با خطا مواجه شد.");
            return await ReturnToIndexWithModelAsync(form);
        }

        TempData["Success"] = "آیتم منو با موفقیت ایجاد شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(NavigationMenuItemFormViewModel form)
    {
        if (!form.Id.HasValue)
        {
            ModelState.AddModelError(string.Empty, "شناسه آیتم معتبر نیست.");
            return await ReturnToIndexWithModelAsync(form);
        }

        if (!ModelState.IsValid)
        {
            return await ReturnToIndexWithModelAsync(form);
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(form.ToUpdateCommand(), cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "به‌روزرسانی آیتم منو با خطا مواجه شد.");
            return await ReturnToIndexWithModelAsync(form);
        }

        TempData["Success"] = "آیتم منو با موفقیت به‌روزرسانی شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new DeleteNavigationMenuItemCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "حذف آیتم منو با خطا مواجه شد.";
        }
        else
        {
            TempData["Success"] = "آیتم منو با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReturnToIndexWithModelAsync(NavigationMenuItemFormViewModel form)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var itemsResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), cancellationToken);

        var items = itemsResult.IsSuccess && itemsResult.Value is not null
            ? itemsResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
            : new List<NavigationMenuItemViewModel>();

        if (!itemsResult.IsSuccess && !string.IsNullOrWhiteSpace(itemsResult.Error))
        {
            TempData["Error"] = itemsResult.Error;
        }

        var model = new NavigationMenuPageViewModel
        {
            Items = items,
            Form = form,
            ParentOptions = BuildParentOptions(items, form.Id)
        };

        SetPageMetadata();
        return View(nameof(Index), model);
    }

    private static IReadOnlyList<SelectListItem> BuildParentOptions(
        IEnumerable<NavigationMenuItemViewModel> items,
        Guid? currentId)
    {
        var options = new List<SelectListItem>
        {
            new SelectListItem
            {
                Value = string.Empty,
                Text = "(بدون والد)"
            }
        };

        foreach (var entry in FlattenWithLevel(items, 0))
        {
            if (currentId.HasValue && entry.Item.Id == currentId.Value)
            {
                continue;
            }

            var indent = entry.Level > 0 ? new string('—', entry.Level) + " " : string.Empty;
            options.Add(new SelectListItem(indent + entry.Item.Title, entry.Item.Id.ToString()));
        }

        return options;
    }

    private static IEnumerable<(NavigationMenuItemViewModel Item, int Level)> FlattenWithLevel(
        IEnumerable<NavigationMenuItemViewModel> items,
        int level)
    {
        foreach (var item in items.OrderBy(i => i.DisplayOrder).ThenBy(i => i.Title))
        {
            yield return (item, level);

            foreach (var child in FlattenWithLevel(item.Children, level + 1))
            {
                yield return child;
            }
        }
    }

    private void SetPageMetadata()
    {
        ViewData["Title"] = "مدیریت منوهای سایت";
        ViewData["Subtitle"] = "تعریف ساختار منوی اصلی و زیرمنوها";
    }
}
