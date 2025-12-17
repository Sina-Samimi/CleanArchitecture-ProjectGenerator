using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Commands.Banners;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Application.Queries.Banners;
using LogTableRenameTest.SharedKernel.Extensions;
using LogTableRenameTest.WebSite.Areas.Admin.Models.Banners;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class BannersController : Controller
{
    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;

    private const string BannerUploadFolder = "banners";
    private const int MaxBannerSizeKb = 5 * 1024;
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/gif"
    };

    public BannersController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        bool? isActive = null,
        bool? showOnHomePage = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "مدیریت بنرها";
        ViewData["Subtitle"] = "افزودن، ویرایش و حذف بنرهای سایت";

        var pageNumber = page < 1 ? 1 : page;
        var pageSize = 20;

        var query = new GetBannersQuery(isActive, showOnHomePage, pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت لیست بنرها با خطا مواجه شد.";
            return View(new BannerListViewModel
            {
                Banners = Array.Empty<BannerViewModel>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = pageSize,
                IsActive = isActive,
                ShowOnHomePage = showOnHomePage
            });
        }

        var data = result.Value;
        var viewModel = new BannerListViewModel
        {
            Banners = data.Items.Select(b => new BannerViewModel
            {
                Id = b.Id,
                Title = b.Title,
                ImagePath = b.ImagePath,
                LinkUrl = b.LinkUrl,
                AltText = b.AltText,
                DisplayOrder = b.DisplayOrder,
                IsActive = b.IsActive,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                ShowOnHomePage = b.ShowOnHomePage,
                CreateDate = b.CreateDate,
                UpdateDate = b.UpdateDate
            }).ToArray(),
            TotalCount = data.TotalCount,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            IsActive = isActive,
            ShowOnHomePage = showOnHomePage
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "افزودن بنر جدید";
        ViewData["Subtitle"] = "ایجاد بنر جدید برای نمایش در سایت";
        return View("Form", new BannerFormViewModel { IsActive = true, ShowOnHomePage = true, DisplayOrder = 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BannerFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateBannerImage(model.Image, nameof(model.Image));

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "افزودن بنر جدید";
            ViewData["Subtitle"] = "ایجاد بنر جدید برای نمایش در سایت";
            return View("Form", model);
        }

        string? imagePath = null;
        if (model.Image is not null)
        {
            imagePath = SaveBannerImage(model.Image);
            if (imagePath is null)
            {
                ModelState.AddModelError(nameof(model.Image), "خطا در آپلود تصویر بنر.");
                ViewData["Title"] = "افزودن بنر جدید";
                ViewData["Subtitle"] = "ایجاد بنر جدید برای نمایش در سایت";
                return View("Form", model);
            }
        }
        else if (string.IsNullOrWhiteSpace(model.ImagePath))
        {
            ModelState.AddModelError(nameof(model.Image), "تصویر بنر الزامی است.");
            ViewData["Title"] = "افزودن بنر جدید";
            ViewData["Subtitle"] = "ایجاد بنر جدید برای نمایش در سایت";
            return View("Form", model);
        }
        else
        {
            imagePath = model.ImagePath;
        }

        // Convert Persian dates to DateTimeOffset
        var startDate = ParsePersianDate(model.StartDatePersian);
        var endDate = ParsePersianDate(model.EndDatePersian);

        var command = new CreateBannerCommand(
            model.Title,
            imagePath,
            model.LinkUrl,
            model.AltText,
            model.DisplayOrder,
            model.IsActive,
            startDate,
            endDate,
            model.ShowOnHomePage);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در ایجاد بنر.");
            ViewData["Title"] = "افزودن بنر جدید";
            ViewData["Subtitle"] = "ایجاد بنر جدید برای نمایش در سایت";
            return View("Form", model);
        }

        TempData["Success"] = "بنر با موفقیت ایجاد شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه بنر معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var query = new GetBannerByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "بنر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var banner = result.Value;
        var viewModel = new BannerFormViewModel
        {
            Id = banner.Id,
            Title = banner.Title,
            ImagePath = banner.ImagePath,
            LinkUrl = banner.LinkUrl,
            AltText = banner.AltText,
            DisplayOrder = banner.DisplayOrder,
            IsActive = banner.IsActive,
            StartDate = banner.StartDate,
            StartDatePersian = banner.StartDate?.ToPersianDateString()?.Replace("۰", "0").Replace("۱", "1").Replace("۲", "2").Replace("۳", "3").Replace("۴", "4").Replace("۵", "5").Replace("۶", "6").Replace("۷", "7").Replace("۸", "8").Replace("۹", "9"),
            EndDate = banner.EndDate,
            EndDatePersian = banner.EndDate?.ToPersianDateString()?.Replace("۰", "0").Replace("۱", "1").Replace("۲", "2").Replace("۳", "3").Replace("۴", "4").Replace("۵", "5").Replace("۶", "6").Replace("۷", "7").Replace("۸", "8").Replace("۹", "9"),
            ShowOnHomePage = banner.ShowOnHomePage
        };

        ViewData["Title"] = "ویرایش بنر";
        ViewData["Subtitle"] = $"ویرایش بنر: {banner.Title}";
        return View("Form", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BannerFormViewModel model, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه بنر معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        ValidateBannerImage(model.Image, nameof(model.Image));

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "ویرایش بنر";
            ViewData["Subtitle"] = "ویرایش اطلاعات بنر";
            return View("Form", model);
        }

        string? imagePath = model.ImagePath;
        if (model.RemoveImage)
        {
            imagePath = null;
        }
        else if (model.Image is not null)
        {
            imagePath = SaveBannerImage(model.Image);
            if (imagePath is null)
            {
                ModelState.AddModelError(nameof(model.Image), "خطا در آپلود تصویر بنر.");
                ViewData["Title"] = "ویرایش بنر";
                ViewData["Subtitle"] = "ویرایش اطلاعات بنر";
                return View("Form", model);
            }
        }

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            ModelState.AddModelError(nameof(model.Image), "تصویر بنر الزامی است.");
            ViewData["Title"] = "ویرایش بنر";
            ViewData["Subtitle"] = "ویرایش اطلاعات بنر";
            return View("Form", model);
        }

        // Convert Persian dates to DateTimeOffset
        var startDate = ParsePersianDate(model.StartDatePersian);
        var endDate = ParsePersianDate(model.EndDatePersian);

        var command = new UpdateBannerCommand(
            id,
            model.Title,
            imagePath,
            model.LinkUrl,
            model.AltText,
            model.DisplayOrder,
            model.IsActive,
            startDate,
            endDate,
            model.ShowOnHomePage);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "خطا در بروزرسانی بنر.");
            ViewData["Title"] = "ویرایش بنر";
            ViewData["Subtitle"] = "ویرایش اطلاعات بنر";
            return View("Form", model);
        }

        TempData["Success"] = "بنر با موفقیت بروزرسانی شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه بنر معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var command = new DeleteBannerCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در حذف بنر.";
        }
        else
        {
            TempData["Success"] = "بنر با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static DateTimeOffset? ParsePersianDate(string? persianDate)
    {
        if (string.IsNullOrWhiteSpace(persianDate))
        {
            return null;
        }

        var normalized = UserFilterFormatting.ParsePersianDate(persianDate, false, out var _);
        return normalized;
    }

    private void ValidateBannerImage(IFormFile? file, string fieldName)
    {
        if (file is null || file.Length == 0)
        {
            return;
        }

        if (!_fileSettingServices.IsFileSizeValid(file, MaxBannerSizeKb))
        {
            ModelState.AddModelError(fieldName, "حجم تصویر بنر باید کمتر از ۵ مگابایت باشد.");
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            ModelState.AddModelError(fieldName, "فرمت تصویر پشتیبانی نمی‌شود.");
        }
    }

    private string? SaveBannerImage(IFormFile file)
    {
        if (!_fileSettingServices.IsFileSizeValid(file, MaxBannerSizeKb))
        {
            return null;
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!AllowedImageContentTypes.Contains(contentType))
        {
            return null;
        }

        var response = _fileSettingServices.UploadImage(BannerUploadFolder, file, Guid.NewGuid().ToString("N"));

        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            return null;
        }

        return response.Data.Replace("\\", "/");
    }
}

