using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Attar.Application.Commands.Admin.AboutSettings;
using Attar.Application.Interfaces;
using Attar.Application.Queries.Admin.AboutSettings;
using Attar.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class AboutSettingsController : Controller
{
    private const string ImageUploadFolder = "about";
    private const int MaxImageSizeKb = 5 * 1024;
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/gif"
    };

    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;

    public AboutSettingsController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetAboutSettingsQuery(), cancellationToken);

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Error))
        {
            TempData["Error"] = result.Error;
        }

        var model = result.IsSuccess && result.Value is not null
            ? AboutSettingsViewModel.FromDto(result.Value)
            : new AboutSettingsViewModel();

        SetPageMetadata();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AboutSettingsViewModel model)
    {
        ValidateImage(model.Image, nameof(AboutSettingsViewModel.Image));

        if (!ModelState.IsValid)
        {
            SetPageMetadata();
            return View(model);
        }

        string? imagePath = model.ImagePath;
        if (model.RemoveImage)
        {
            imagePath = null;
        }
        else if (model.Image is not null)
        {
            imagePath = SaveImage(model.Image, nameof(AboutSettingsViewModel.Image));
            if (imagePath is null)
            {
                SetPageMetadata();
                return View(model);
            }
        }

        model.ImagePath = imagePath;

        var cancellationToken = HttpContext.RequestAborted;
        var command = model.ToCommand();
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "در ذخیره‌سازی تنظیمات صفحه درباره ما خطایی رخ داد.");
            SetPageMetadata();
            return View(model);
        }

        TempData["Success"] = "تنظیمات صفحه درباره ما با موفقیت ذخیره شد.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateImage(IFormFile? file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return;
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxImageSizeKb))
        {
            ModelState.AddModelError(fieldName, "حجم تصویر باید کمتر از ۵ مگابایت باشد.");
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            ModelState.AddModelError(fieldName, "فرمت تصویر پشتیبانی نمی‌شود.");
        }
    }

    private string? SaveImage(IFormFile file, string fieldName)
    {
        var response = _fileSettingServices.UploadImage(ImageUploadFolder, file, Guid.NewGuid().ToString("N"));

        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            ModelState.AddModelError(fieldName, response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی تصویر وجود ندارد.");
            return null;
        }

        return response.Data.Replace("\\", "/");
    }

    private void SetPageMetadata()
    {
        ViewData["Title"] = "تنظیمات صفحه درباره ما";
        ViewData["Subtitle"] = "مدیریت محتوای صفحه درباره ما";
    }
}

