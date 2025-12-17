using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Attar.Application.Commands.Admin.SiteSettings;
using Attar.Application.Interfaces;
using Attar.Application.Queries.Admin.SiteSettings;
using Attar.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class SiteSettingsController : Controller
{
    private const string LogoUploadFolder = "site/logo";
    private const string FaviconUploadFolder = "site/favicon";
    private const string ContentUploadFolder = "site/content";
    private const int MaxLogoSizeKb = 2 * 1024;
    private const int MaxFaviconSizeKb = 500;
    private const int MaxEditorImageSizeKb = 5 * 1024;
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/gif",
        "image/x-icon",
        "image/vnd.microsoft.icon"
    };

    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;

    public SiteSettingsController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
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
        ValidateLogo(model.Logo, nameof(SiteSettingsViewModel.Logo));
        ValidateFavicon(model.Favicon, nameof(SiteSettingsViewModel.Favicon));

        if (!ModelState.IsValid)
        {
            SetPageMetadata();
            return View(model);
        }

        string? logoPath = model.LogoPath;
        if (model.RemoveLogo)
        {
            logoPath = null;
        }
        else if (model.Logo is not null)
        {
            logoPath = SaveLogo(model.Logo, nameof(SiteSettingsViewModel.Logo));
            if (logoPath is null)
            {
                SetPageMetadata();
                return View(model);
            }
        }

        string? faviconPath = model.FaviconPath;
        if (model.RemoveFavicon)
        {
            faviconPath = null;
        }
        else if (model.Favicon is not null)
        {
            faviconPath = SaveFavicon(model.Favicon, nameof(SiteSettingsViewModel.Favicon));
            if (faviconPath is null)
            {
                SetPageMetadata();
                return View(model);
            }
        }

        model.LogoPath = logoPath;
        model.FaviconPath = faviconPath;

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

    [HttpPost]
    [ValidateAntiForgeryToken]
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

    private void ValidateLogo(IFormFile? file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return;
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxLogoSizeKb))
        {
            ModelState.AddModelError(fieldName, "حجم لوگو باید کمتر از ۲ مگابایت باشد.");
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            ModelState.AddModelError(fieldName, "فرمت تصویر پشتیبانی نمی‌شود.");
        }
    }

    private void ValidateFavicon(IFormFile? file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return;
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxFaviconSizeKb))
        {
            ModelState.AddModelError(fieldName, "حجم Favicon باید کمتر از ۵۰۰ کیلوبایت باشد.");
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            ModelState.AddModelError(fieldName, "فرمت تصویر پشتیبانی نمی‌شود.");
        }
    }

    private string? SaveLogo(IFormFile file, string fieldName)
    {
        var response = _fileSettingServices.UploadImage(LogoUploadFolder, file, Guid.NewGuid().ToString("N"));

        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            ModelState.AddModelError(fieldName, response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی لوگو وجود ندارد.");
            return null;
        }

        return response.Data.Replace("\\", "/");
    }

    private string? SaveFavicon(IFormFile file, string fieldName)
    {
        var response = _fileSettingServices.UploadImage(FaviconUploadFolder, file, Guid.NewGuid().ToString("N"));

        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            ModelState.AddModelError(fieldName, response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی Favicon وجود ندارد.");
            return null;
        }

        return response.Data.Replace("\\", "/");
    }


    private void SetPageMetadata()
    {
        ViewData["Title"] = "تنظیمات سایت";
        ViewData["Subtitle"] = "مدیریت اطلاعات تماس و برند پلتفرم";
    }
}
