using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Commands.Pages;
using LogTableRenameTest.Application.DTOs.Pages;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Application.Queries.Pages;
using LogTableRenameTest.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogTableRenameTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class PagesController : Controller
{
    private const string ContentUploadFolder = "pages/content";
    private const int MaxEditorImageSizeKb = 5 * 1024;
    private const string FeaturedUploadFolder = "pages/featured";
    private const int MaxFeaturedImageSizeKb = 5 * 1024;
    private const string DefaultRobots = "index,follow";
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif"
    };
    private static readonly string[] RobotsOptions =
    {
        "index,follow",
        "index,nofollow",
        "noindex,follow",
        "noindex,nofollow"
    };

    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;

    public PagesController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool? published)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var query = new GetPagesQuery(published);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت لیست صفحات";
            return View(new PagesIndexViewModel());
        }

        // Get page statistics
        var statisticsQuery = new GetPageStatisticsQuery();
        var statisticsResult = await _mediator.Send(statisticsQuery, cancellationToken);
        var statistics = statisticsResult.IsSuccess && statisticsResult.Value is not null
            ? statisticsResult.Value
            : new Application.DTOs.Pages.PageStatisticsDto(0, 0, 0, 0, 0, 0);

        var pages = result.Value?.Pages ?? Array.Empty<PageListItemDto>();
        var viewModel = new PagesIndexViewModel
        {
            Pages = pages.Select(p => new PageListItemViewModel(
                p.Id,
                p.Title,
                p.Slug,
                p.IsPublished,
                p.PublishedAt,
                p.ViewCount,
                p.CreateDate)).ToArray(),
            TotalCount = result.Value?.TotalCount ?? 0,
            PublishedFilter = published,
            Statistics = statistics
        };

        ViewData["Title"] = "مدیریت صفحات";
        ViewData["Subtitle"] = "ایجاد و مدیریت صفحات داینامیک";

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var viewModel = new PageFormViewModel
        {
            Selections = BuildFormSelections(null)
        };
        ViewData["Title"] = "ایجاد صفحه جدید";
        ViewData["Subtitle"] = "افزودن صفحه داینامیک جدید";
        return View("Form", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var query = new GetPageByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "صفحه مورد نظر یافت نشد";
            return RedirectToAction(nameof(Index));
        }

        var dto = result.Value;
        var normalizedRobots = NormalizeRobots(dto.MetaRobots);
        var viewModel = new PageFormViewModel
        {
            Id = dto.Id,
            Title = dto.Title,
            Slug = dto.Slug,
            Content = dto.Content,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            MetaKeywords = dto.MetaKeywords,
            MetaRobots = normalizedRobots,
            IsPublished = dto.IsPublished,
            FeaturedImagePath = dto.FeaturedImagePath,
            ShowInFooter = dto.ShowInFooter,
            ShowInQuickAccess = dto.ShowInQuickAccess,
            Selections = BuildFormSelections(normalizedRobots)
        };

        ViewData["Title"] = "ویرایش صفحه";
        ViewData["Subtitle"] = $"ویرایش صفحه: {dto.Title}";

        return View("Form", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PageFormViewModel model)
    {
        ValidateFeaturedImage(model.FeaturedImage, nameof(PageFormViewModel.FeaturedImage));

        if (!ModelState.IsValid)
        {
            model.Selections = BuildFormSelections(model.MetaRobots);
            ViewData["Title"] = "ایجاد صفحه جدید";
            ViewData["Subtitle"] = "افزودن صفحه داینامیک جدید";
            return View("Form", model);
        }

        var cancellationToken = HttpContext.RequestAborted;
        var normalizedRobots = NormalizeRobots(model.MetaRobots);

        string? featuredImagePath = null;
        if (model.FeaturedImage is not null)
        {
            featuredImagePath = SaveFeaturedImage(model.FeaturedImage, nameof(PageFormViewModel.FeaturedImage));
            if (featuredImagePath is null)
            {
                model.Selections = BuildFormSelections(normalizedRobots);
                ViewData["Title"] = "ایجاد صفحه جدید";
                ViewData["Subtitle"] = "افزودن صفحه داینامیک جدید";
                return View("Form", model);
            }
        }

        var command = new CreatePageCommand(
            model.Title,
            model.Slug,
            model.Content,
            model.MetaTitle,
            model.MetaDescription,
            model.MetaKeywords,
            normalizedRobots,
            model.IsPublished,
            featuredImagePath,
            model.ShowInFooter,
            model.ShowInQuickAccess);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (!string.IsNullOrWhiteSpace(featuredImagePath))
            {
                _fileSettingServices.DeleteFile(featuredImagePath);
            }
            model.Selections = BuildFormSelections(normalizedRobots);
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در ایجاد صفحه");
            ViewData["Title"] = "ایجاد صفحه جدید";
            ViewData["Subtitle"] = "افزودن صفحه داینامیک جدید";
            return View("Form", model);
        }

        TempData["Success"] = "صفحه با موفقیت ایجاد شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PageFormViewModel model)
    {
        ValidateFeaturedImage(model.FeaturedImage, nameof(PageFormViewModel.FeaturedImage));

        if (!ModelState.IsValid)
        {
            model.Selections = BuildFormSelections(model.MetaRobots);
            ViewData["Title"] = "ویرایش صفحه";
            ViewData["Subtitle"] = $"ویرایش صفحه: {model.Title}";
            return View("Form", model);
        }

        if (!model.Id.HasValue)
        {
            model.Selections = BuildFormSelections(model.MetaRobots);
            ModelState.AddModelError(string.Empty, "شناسه صفحه نامعتبر است");
            return View("Form", model);
        }

        var cancellationToken = HttpContext.RequestAborted;
        var normalizedRobots = NormalizeRobots(model.MetaRobots);

        var previousImagePath = model.FeaturedImagePath;
        string? uploadedImagePath = null;
        if (model.FeaturedImage is not null)
        {
            uploadedImagePath = SaveFeaturedImage(model.FeaturedImage, nameof(PageFormViewModel.FeaturedImage));
            if (uploadedImagePath is null)
            {
                model.Selections = BuildFormSelections(normalizedRobots);
                ViewData["Title"] = "ویرایش صفحه";
                ViewData["Subtitle"] = $"ویرایش صفحه: {model.Title}";
                return View("Form", model);
            }
        }

        var nextImagePath = model.RemoveFeaturedImage ? null : (uploadedImagePath ?? previousImagePath);

        var command = new UpdatePageCommand(
            model.Id.Value,
            model.Title,
            model.Slug,
            model.Content,
            model.MetaTitle,
            model.MetaDescription,
            model.MetaKeywords,
            normalizedRobots,
            model.IsPublished,
            nextImagePath,
            model.ShowInFooter,
            model.ShowInQuickAccess);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (!string.IsNullOrWhiteSpace(uploadedImagePath))
            {
                _fileSettingServices.DeleteFile(uploadedImagePath);
            }
            model.Selections = BuildFormSelections(normalizedRobots);
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در ویرایش صفحه");
            ViewData["Title"] = "ویرایش صفحه";
            ViewData["Subtitle"] = $"ویرایش صفحه: {model.Title}";
            return View("Form", model);
        }

        // Delete old image if it was replaced or removed
        if (model.RemoveFeaturedImage && !string.IsNullOrWhiteSpace(previousImagePath))
        {
            _fileSettingServices.DeleteFile(previousImagePath);
        }
        else if (!string.IsNullOrWhiteSpace(uploadedImagePath) && !string.IsNullOrWhiteSpace(previousImagePath) && previousImagePath != uploadedImagePath)
        {
            _fileSettingServices.DeleteFile(previousImagePath);
        }

        TempData["Success"] = "صفحه با موفقیت به‌روزرسانی شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var command = new DeletePageCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در حذف صفحه";
        }
        else
        {
            TempData["Success"] = "صفحه با موفقیت حذف شد";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult UploadContentImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "فایلی برای آپلود ارسال نشده است." });
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxEditorImageSizeKb))
        {
            return BadRequest(new { error = "حجم تصویر باید کمتر از ۵ مگابایت باشد." });
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            return BadRequest(new { error = "فرمت تصویر پشتیبانی نمی‌شود." });
        }

        var response = _fileSettingServices.UploadImage(ContentUploadFolder, file, Guid.NewGuid().ToString("N"));
        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            return BadRequest(new { error = response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی تصویر وجود ندارد." });
        }

        var normalizedPath = response.Data.Replace("\\", "/");
        if (!normalizedPath.StartsWith('/'))
        {
            normalizedPath = "/" + normalizedPath;
        }

        return Json(new { url = normalizedPath });
    }

    private static PageFormSelectionsViewModel BuildFormSelections(string? selectedRobots)
        => new()
        {
            RobotsOptions = BuildRobotsOptions(selectedRobots)
        };

    private static IReadOnlyCollection<SelectListItem> BuildRobotsOptions(string? selectedRobots)
    {
        var selected = NormalizeRobots(selectedRobots);

        return RobotsOptions
            .Select(option => new SelectListItem(
                GetRobotsDisplay(option),
                option,
                string.Equals(option, selected, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private static string GetRobotsDisplay(string robots)
        => robots switch
        {
            "index,follow" => "ایندکس و دنبال",
            "index,nofollow" => "ایندکس و عدم دنبال",
            "noindex,follow" => "عدم ایندکس و دنبال",
            "noindex,nofollow" => "عدم ایندکس و عدم دنبال",
            _ => robots
        };

    private static string NormalizeRobots(string? robots)
    {
        if (string.IsNullOrWhiteSpace(robots))
        {
            return DefaultRobots;
        }

        var sanitized = robots.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();
        return RobotsOptions.FirstOrDefault(option => string.Equals(option, sanitized, StringComparison.OrdinalIgnoreCase))
            ?? sanitized.ToLowerInvariant();
    }

    private void ValidateFeaturedImage(IFormFile? file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return;
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxFeaturedImageSizeKb))
        {
            ModelState.AddModelError(fieldName, "حجم تصویر باید کمتر از ۵ مگابایت باشد.");
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            ModelState.AddModelError(fieldName, "فرمت تصویر پشتیبانی نمی‌شود.");
        }
    }

    private string? SaveFeaturedImage(IFormFile file, string fieldName)
    {
        var response = _fileSettingServices.UploadImage(FeaturedUploadFolder, file, Guid.NewGuid().ToString("N"));

        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            ModelState.AddModelError(fieldName, response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی تصویر وجود ندارد.");
            return null;
        }

        return response.Data.Replace("\\", "/");
    }
}

