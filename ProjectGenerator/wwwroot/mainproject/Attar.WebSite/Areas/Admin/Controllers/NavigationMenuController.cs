using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Attar.Application.Commands.Admin.NavigationMenu;
using Attar.Application.Interfaces;
using Attar.Application.Queries.Admin.NavigationMenu;
using Attar.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attar.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class NavigationMenuController : Controller
{
    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;
    private const string MenuImageUploadFolder = "navigation-menu";
    private const int MaxImageSizeKb = 100;
    private static readonly string[] AllowedImageContentTypes = { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };

    public NavigationMenuController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] NavigationMenuFilterViewModel? filter, [FromQuery] int page = 1)
    {
        filter ??= new NavigationMenuFilterViewModel();
        var cancellationToken = HttpContext.RequestAborted;

        // Get tree structure for display
        var treeResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), cancellationToken);
        var treeItems = treeResult.IsSuccess && treeResult.Value is not null
            ? treeResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
            : new List<NavigationMenuItemViewModel>();
        var allItems = treeItems;

        // Flatten and filter
        var allFlattened = treeItems.SelectMany(item => item.Flatten()).ToList();
        var filtered = allFlattened.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchTerm = filter.Search.Trim();
            filtered = filtered.Where(item =>
                item.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(item.Url) && item.Url.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        if (filter.ParentId.HasValue)
        {
            filtered = filtered.Where(item => item.ParentId == filter.ParentId.Value);
        }
        else if (filter.ParentId == Guid.Empty)
        {
            filtered = filtered.Where(item => !item.ParentId.HasValue);
        }

        if (filter.IsVisible.HasValue)
        {
            filtered = filtered.Where(item => item.IsVisible == filter.IsVisible.Value);
        }

        var filteredList = filtered.ToList();
        var filteredCount = filteredList.Count;

        // Apply pagination
        var pageNumber = page <= 0 ? 1 : page;
        var pageSize = filter.PageSize <= 0 ? 20 : Math.Clamp(filter.PageSize, 5, 100);
        var totalPages = filteredCount == 0 ? 1 : (int)Math.Ceiling(filteredCount / (double)pageSize);

        var pagedItems = filteredList
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new NavigationMenuItemListItemViewModel
            {
                Id = item.Id,
                ParentId = item.ParentId,
                Title = item.Title,
                Url = item.Url,
                Icon = item.Icon,
                ImageUrl = item.ImageUrl,
                IsVisible = item.IsVisible,
                OpenInNewTab = item.OpenInNewTab,
                DisplayOrder = item.DisplayOrder,
                ParentTitle = treeItems.SelectMany(i => i.Flatten())
                    .FirstOrDefault(p => p.Id == item.ParentId)?.Title,
                ChildrenCount = item.Children.Count
            })
            .ToList();

        var items = pagedItems;

        var model = new NavigationMenuPageViewModel
        {
            Items = items,
            Filter = filter,
            TotalCount = allFlattened.Count,
            FilteredCount = filteredCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            ParentOptions = BuildParentOptions(allItems, null),
            TreeItems = treeItems
        };

        SetPageMetadata();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetForm([FromQuery] Guid? id)
    {
        var cancellationToken = HttpContext.RequestAborted;
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
                return Json(new { success = false, error = itemResult.Error });
            }
        }

        // Get all items for parent options
        var treeResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), cancellationToken);
        var allItems = treeResult.IsSuccess && treeResult.Value is not null
            ? treeResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
            : new List<NavigationMenuItemViewModel>();

        var parentOptions = BuildParentOptions(allItems, form.Id);

        return PartialView("_FormModal", new NavigationMenuFormModalViewModel
        {
            Form = form,
            ParentOptions = parentOptions
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NavigationMenuItemFormViewModel form)
    {
        // Validate and save image
        string? imageUrl = null;
        if (form.ImageFile is not null && form.ImageFile.Length > 0)
        {
            if (!ValidateImageFile(form.ImageFile, nameof(form.ImageFile)))
            {
                var treeResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), HttpContext.RequestAborted);
                var allItems = treeResult.IsSuccess && treeResult.Value is not null
                    ? treeResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
                    : new List<NavigationMenuItemViewModel>();

                return PartialView("_FormModal", new NavigationMenuFormModalViewModel
                {
                    Form = form,
                    ParentOptions = BuildParentOptions(allItems, form.Id)
                });
            }

            imageUrl = await SaveImageAsync(form.ImageFile, nameof(form.ImageFile));
            if (imageUrl is null)
            {
                var treeResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), HttpContext.RequestAborted);
                var allItems = treeResult.IsSuccess && treeResult.Value is not null
                    ? treeResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
                    : new List<NavigationMenuItemViewModel>();

                return PartialView("_FormModal", new NavigationMenuFormModalViewModel
                {
                    Form = form,
                    ParentOptions = BuildParentOptions(allItems, form.Id)
                });
            }
        }

        if (!ModelState.IsValid)
        {
            var treeResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), HttpContext.RequestAborted);
            var allItems = treeResult.IsSuccess && treeResult.Value is not null
                ? treeResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
                : new List<NavigationMenuItemViewModel>();

            return PartialView("_FormModal", new NavigationMenuFormModalViewModel
            {
                Form = form,
                ParentOptions = BuildParentOptions(allItems, form.Id)
            });
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = new CreateNavigationMenuItemCommand(
            form.Title,
            form.Url,
            form.Icon ?? string.Empty,
            form.IsVisible,
            form.OpenInNewTab,
            form.DisplayOrder,
            form.ParentId,
            imageUrl);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "ثبت آیتم منو با خطا مواجه شد.");
            var treeResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), cancellationToken);
            var allItems = treeResult.IsSuccess && treeResult.Value is not null
                ? treeResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
                : new List<NavigationMenuItemViewModel>();

            return PartialView("_FormModal", new NavigationMenuFormModalViewModel
            {
                Form = form,
                ParentOptions = BuildParentOptions(allItems, form.Id)
            });
        }

        TempData["Success"] = "آیتم منو با موفقیت ایجاد شد.";
        return Json(new { success = true, message = "آیتم منو با موفقیت ایجاد شد." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(NavigationMenuItemFormViewModel form)
    {
        if (!form.Id.HasValue)
        {
            ModelState.AddModelError(string.Empty, "شناسه آیتم معتبر نیست.");
            var treeResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), HttpContext.RequestAborted);
            var allItems = treeResult.IsSuccess && treeResult.Value is not null
                ? treeResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
                : new List<NavigationMenuItemViewModel>();

            return PartialView("_FormModal", new NavigationMenuFormModalViewModel
            {
                Form = form,
                ParentOptions = BuildParentOptions(allItems, form.Id)
            });
        }

        // Get current item to preserve existing image if new one is not uploaded
        var currentItemResult = await _mediator.Send(new GetNavigationMenuItemQuery(form.Id.Value), HttpContext.RequestAborted);
        var currentImageUrl = currentItemResult.IsSuccess && currentItemResult.Value is not null
            ? currentItemResult.Value.ImageUrl
            : null;

        // Check if user wants to remove image
        var removeImage = Request.Form["removeImage"].ToString() == "true";
        string? imageUrl = null;

        if (removeImage)
        {
            // Delete old image if exists
            if (!string.IsNullOrWhiteSpace(currentImageUrl))
            {
                _fileSettingServices.DeleteFile(currentImageUrl);
            }
            imageUrl = null;
        }
        else if (form.ImageFile is not null && form.ImageFile.Length > 0)
        {
            // Validate and save new image
            if (!ValidateImageFile(form.ImageFile, nameof(form.ImageFile)))
            {
                var treeResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), HttpContext.RequestAborted);
                var allItems = treeResult.IsSuccess && treeResult.Value is not null
                    ? treeResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
                    : new List<NavigationMenuItemViewModel>();

                return PartialView("_FormModal", new NavigationMenuFormModalViewModel
                {
                    Form = form,
                    ParentOptions = BuildParentOptions(allItems, form.Id)
                });
            }

            // Delete old image if exists
            if (!string.IsNullOrWhiteSpace(currentImageUrl))
            {
                _fileSettingServices.DeleteFile(currentImageUrl);
            }

            imageUrl = await SaveImageAsync(form.ImageFile, nameof(form.ImageFile));
            if (imageUrl is null)
            {
                var treeResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), HttpContext.RequestAborted);
                var allItems = treeResult.IsSuccess && treeResult.Value is not null
                    ? treeResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
                    : new List<NavigationMenuItemViewModel>();

                return PartialView("_FormModal", new NavigationMenuFormModalViewModel
                {
                    Form = form,
                    ParentOptions = BuildParentOptions(allItems, form.Id)
                });
            }
        }
        else
        {
            // Keep existing image
            imageUrl = currentImageUrl;
        }

        if (!ModelState.IsValid)
        {
            var treeResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), HttpContext.RequestAborted);
            var allItems = treeResult.IsSuccess && treeResult.Value is not null
                ? treeResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
                : new List<NavigationMenuItemViewModel>();

            return PartialView("_FormModal", new NavigationMenuFormModalViewModel
            {
                Form = form,
                ParentOptions = BuildParentOptions(allItems, form.Id)
            });
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = new UpdateNavigationMenuItemCommand(
            form.Id.Value,
            form.Title,
            form.Url,
            form.Icon ?? string.Empty,
            form.IsVisible,
            form.OpenInNewTab,
            form.DisplayOrder,
            form.ParentId,
            imageUrl);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "به‌روزرسانی آیتم منو با خطا مواجه شد.");
            var treeResult = await _mediator.Send(new GetNavigationMenuTreeQuery(), cancellationToken);
            var allItems = treeResult.IsSuccess && treeResult.Value is not null
                ? treeResult.Value.Select(NavigationMenuItemViewModel.FromDto).ToList()
                : new List<NavigationMenuItemViewModel>();

            return PartialView("_FormModal", new NavigationMenuFormModalViewModel
            {
                Form = form,
                ParentOptions = BuildParentOptions(allItems, form.Id)
            });
        }

        TempData["Success"] = "آیتم منو با موفقیت به‌روزرسانی شد.";
        return Json(new { success = true, message = "آیتم منو با موفقیت به‌روزرسانی شد." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new DeleteNavigationMenuItemCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error ?? "حذف آیتم منو با خطا مواجه شد." });
        }

        TempData["Success"] = "آیتم منو با موفقیت حذف شد.";
        return Json(new { success = true, message = "آیتم منو با موفقیت حذف شد." });
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

    private bool ValidateImageFile(IFormFile file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return true;
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxImageSizeKb))
        {
            ModelState.AddModelError(fieldName, $"حجم تصویر باید کمتر از {MaxImageSizeKb} کیلوبایت باشد.");
            return false;
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            ModelState.AddModelError(fieldName, "فقط فرمت‌های تصویر (JPG, PNG, WEBP, GIF) پشتیبانی می‌شوند.");
            return false;
        }

        return true;
    }

    private async Task<string?> SaveImageAsync(IFormFile file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        try
        {
            var response = _fileSettingServices.UploadImage(MenuImageUploadFolder, file, Guid.NewGuid().ToString("N"));

            if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
            {
                var errorMessage = response.Messages?.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی تصویر وجود ندارد.";
                ModelState.AddModelError(fieldName, errorMessage);
                return null;
            }

            return response.Data.Replace("\\", "/");
        }
        catch
        {
            ModelState.AddModelError(fieldName, "ذخیره‌سازی تصویر انجام نشد.");
            return null;
        }
    }

    private void SetPageMetadata()
    {
        ViewData["Title"] = "مدیریت منوهای سایت";
        ViewData["Subtitle"] = "تعریف ساختار منوی اصلی و زیرمنوها";
    }
}
