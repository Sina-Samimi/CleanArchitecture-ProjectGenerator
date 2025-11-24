using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Commands.Teachers;
using Arsis.Application.Interfaces;
using Arsis.Application.Queries.Identity.GetUserLookups;
using Arsis.Application.Queries.Teachers;
using EndPoint.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class TeachersController : Controller
{
    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;

    private const int MaxAvatarFileSizeKb = 2 * 1024;
    private static readonly string[] AllowedAvatarContentTypes =
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private const string TeacherAvatarUploadFolder = "teacher-profiles";

    public TeachersController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ConfigurePageMetadata("مدیریت مدرسین", "تعریف و ویرایش اطلاعات مدرسین سایت");

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetTeacherProfilesQuery(), cancellationToken);

        TeacherProfilesIndexViewModel viewModel;
        if (!result.IsSuccess || result.Value is null)
        {
            var items = Array.Empty<TeacherProfileListItemViewModel>();
            viewModel = new TeacherProfilesIndexViewModel
            {
                Teachers = items,
                ActiveCount = 0,
                InactiveCount = 0,
                ErrorMessage = result.Error ?? TempData["Teachers.Error"] as string,
                SuccessMessage = TempData["Teachers.Success"] as string
            };
        }
        else
        {
            var dto = result.Value;
            var items = dto.Items
                .Select(item => new TeacherProfileListItemViewModel(
                    item.Id,
                    item.DisplayName,
                    item.Degree,
                    item.Specialty,
                    item.Bio,
                    item.ContactEmail,
                    item.ContactPhone,
                    item.UserId,
                    item.IsActive,
                    item.CreatedAt,
                    item.UpdatedAt))
                .ToArray();

            viewModel = new TeacherProfilesIndexViewModel
            {
                Teachers = items,
                ActiveCount = dto.ActiveCount,
                InactiveCount = dto.InactiveCount,
                SuccessMessage = TempData["Teachers.Success"] as string,
                ErrorMessage = TempData["Teachers.Error"] as string
            };
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ConfigurePageMetadata("افزودن مدرس جدید", "ثبت اطلاعات مدرسان برای استفاده در محصولات و پنل مدرس");

        var model = new TeacherProfileFormViewModel();
        await PopulateUserOptionsAsync(model, HttpContext.RequestAborted);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeacherProfileFormViewModel model)
    {
        ConfigurePageMetadata("افزودن مدرس جدید", "ثبت اطلاعات مدرسان برای استفاده در محصولات و پنل مدرس");

        NormalizeForm(model);

        var cancellationToken = HttpContext.RequestAborted;
        await PopulateUserOptionsAsync(model, cancellationToken);

        ValidateAvatar(model.AvatarFile, nameof(model.AvatarFile));

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var uploadedAvatarPath = await SaveAvatarAsync(model.AvatarFile, nameof(model.AvatarFile));

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var avatarPath = uploadedAvatarPath ?? model.AvatarUrl;

        var command = new CreateTeacherProfileCommand(
            model.DisplayName,
            model.Degree,
            model.Specialty,
            model.Bio,
            avatarPath,
            model.ContactEmail,
            model.ContactPhone,
            model.UserId,
            model.IsActive);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            if (!string.IsNullOrWhiteSpace(uploadedAvatarPath))
            {
                DeleteAvatarFile(uploadedAvatarPath);
            }

            ModelState.AddModelError(string.Empty, result.Error ?? "ثبت مدرس با خطا مواجه شد.");
            return View("Form", model);
        }

        TempData["Teachers.Success"] = "مدرس جدید با موفقیت ثبت شد.";
        TempData["Alert.Message"] = "پروفایل مدرس ایجاد شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        ConfigurePageMetadata("ویرایش اطلاعات مدرس", "به‌روزرسانی مشخصات و اطلاعات تماس مدرس");

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetTeacherProfileDetailQuery(id), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Teachers.Error"] = result.Error ?? "مدرس مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var dto = result.Value;
        var model = new TeacherProfileFormViewModel
        {
            Id = dto.Id,
            DisplayName = dto.DisplayName,
            Degree = dto.Degree,
            Specialty = dto.Specialty,
            Bio = dto.Bio,
            AvatarUrl = dto.AvatarUrl,
            OriginalAvatarUrl = dto.AvatarUrl,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            UserId = dto.UserId,
            IsActive = dto.IsActive
        };

        await PopulateUserOptionsAsync(model, HttpContext.RequestAborted);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TeacherProfileFormViewModel model)
    {
        ConfigurePageMetadata("ویرایش اطلاعات مدرس", "به‌روزرسانی مشخصات و اطلاعات تماس مدرس");

        if (id != model.Id)
        {
            ModelState.AddModelError(string.Empty, "شناسه مدرس معتبر نیست.");
        }

        NormalizeForm(model);

        var cancellationToken = HttpContext.RequestAborted;
        await PopulateUserOptionsAsync(model, cancellationToken);

        ValidateAvatar(model.AvatarFile, nameof(model.AvatarFile));

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var uploadedAvatarPath = await SaveAvatarAsync(model.AvatarFile, nameof(model.AvatarFile));

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var previousAvatarPath = model.OriginalAvatarUrl;
        var avatarPath = uploadedAvatarPath ?? model.AvatarUrl;

        var command = new UpdateTeacherProfileCommand(
            id,
            model.DisplayName,
            model.Degree,
            model.Specialty,
            model.Bio,
            avatarPath,
            model.ContactEmail,
            model.ContactPhone,
            model.UserId,
            model.IsActive);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            if (!string.IsNullOrWhiteSpace(uploadedAvatarPath))
            {
                DeleteAvatarFile(uploadedAvatarPath);
            }

            ModelState.AddModelError(string.Empty, result.Error ?? "ویرایش مدرس با خطا مواجه شد.");
            return View("Form", model);
        }

        if (!string.IsNullOrWhiteSpace(uploadedAvatarPath) &&
            !string.IsNullOrWhiteSpace(previousAvatarPath) &&
            !string.Equals(previousAvatarPath, uploadedAvatarPath, StringComparison.OrdinalIgnoreCase))
        {
            DeleteAvatarFile(previousAvatarPath);
        }

        TempData["Teachers.Success"] = "اطلاعات مدرس با موفقیت به‌روزرسانی شد.";
        TempData["Alert.Message"] = "پروفایل مدرس ویرایش شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new ActivateTeacherCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Teachers.Error"] = result.Error ?? "فعال‌سازی مدرس با خطا مواجه شد.";
        }
        else
        {
            TempData["Teachers.Success"] = "پروفایل مدرس فعال شد.";
            TempData["Alert.Title"] = "وضعیت مدرس";
            TempData["Alert.Message"] = "دسترسی مدرس دوباره فعال شد و امکان ورود برای او فراهم است.";
            TempData["Alert.Type"] = "success";
            TempData["Alert.ConfirmText"] = "باشه";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new DeactivateTeacherCommand(id, null), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Teachers.Error"] = result.Error ?? "غیرفعال‌سازی مدرس با خطا مواجه شد.";
        }
        else
        {
            TempData["Teachers.Success"] = "پروفایل مدرس غیرفعال شد.";
            TempData["Alert.Title"] = "وضعیت مدرس";
            TempData["Alert.Message"] = "دسترسی مدرس غیرفعال شد. برای فعال‌سازی مجدد از همین بخش اقدام کنید.";
            TempData["Alert.Type"] = "warning";
            TempData["Alert.ConfirmText"] = "متوجه شدم";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new RemoveTeacherProfileCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Teachers.Error"] = result.Error ?? "حذف مدرس با خطا مواجه شد.";
        }
        else
        {
            TempData["Teachers.Success"] = "پروفایل مدرس با موفقیت حذف شد.";
            TempData["Alert.Title"] = "حذف مدرس";
            TempData["Alert.Message"] = "مدرس از نقش مدرس خارج شد و از سیستم خارج گردید.";
            TempData["Alert.Type"] = "success";
            TempData["Alert.ConfirmText"] = "باشه";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateUserOptionsAsync(TeacherProfileFormViewModel model, CancellationToken cancellationToken)
    {
        if (model is null)
        {
            return;
        }

        var result = await _mediator.Send(new GetUserLookupsQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            model.UserOptions = Array.Empty<SelectListItem>();
            return;
        }

        var options = new List<SelectListItem>(result.Value.Count);

        foreach (var userLookup in result.Value)
        {
            var text = userLookup.DisplayName;
            if (!string.IsNullOrWhiteSpace(userLookup.Email) &&
                !string.Equals(userLookup.Email, userLookup.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                text = $"{userLookup.DisplayName} ({userLookup.Email})";
            }

            if (!userLookup.IsActive)
            {
                text = $"{text} (غیرفعال)";
            }

            options.Add(new SelectListItem
            {
                Value = userLookup.Id,
                Text = text
            });
        }

        if (!string.IsNullOrWhiteSpace(model.UserId) && options.All(option => option.Value != model.UserId))
        {
            options.Insert(0, new SelectListItem
            {
                Value = model.UserId,
                Text = $"{model.UserId} (کاربر یافت نشد یا غیرفعال است)"
            });
        }

        model.UserOptions = options;
    }

    private static void NormalizeForm(TeacherProfileFormViewModel model)
    {
        if (model is null)
        {
            return;
        }

        model.DisplayName = model.DisplayName?.Trim() ?? string.Empty;
        model.Degree = string.IsNullOrWhiteSpace(model.Degree) ? null : model.Degree.Trim();
        model.Specialty = string.IsNullOrWhiteSpace(model.Specialty) ? null : model.Specialty.Trim();
        model.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
        model.AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl.Trim();
        model.ContactEmail = string.IsNullOrWhiteSpace(model.ContactEmail) ? null : model.ContactEmail.Trim();
        model.ContactPhone = string.IsNullOrWhiteSpace(model.ContactPhone) ? null : model.ContactPhone.Trim();
        model.UserId = string.IsNullOrWhiteSpace(model.UserId) ? null : model.UserId.Trim();
        model.OriginalAvatarUrl = string.IsNullOrWhiteSpace(model.OriginalAvatarUrl) ? null : model.OriginalAvatarUrl.Trim();
    }

    private void ConfigurePageMetadata(string title, string subtitle)
    {
        ViewData["Title"] = title;
        ViewData["Subtitle"] = subtitle;
        ViewData["SearchPlaceholder"] = "جستجوی مدرس";
        ViewData["ShowSearch"] = false;
    }

    private bool ValidateAvatar(IFormFile? avatarFile, string propertyName)
    {
        if (avatarFile is null || avatarFile.Length == 0)
        {
            return true;
        }

        if (!_fileSettingServices.IsFileSizeValid(avatarFile, MaxAvatarFileSizeKb))
        {
            ModelState.AddModelError(propertyName, "حجم تصویر باید کمتر از ۲ مگابایت باشد.");
            return false;
        }

        var contentType = avatarFile.ContentType ?? string.Empty;
        if (!AllowedAvatarContentTypes.Contains(contentType))
        {
            ModelState.AddModelError(propertyName, "تنها فرمت‌های PNG، JPG و WEBP مجاز هستند.");
            return false;
        }

        return true;
    }

    private Task<string?> SaveAvatarAsync(IFormFile? avatarFile, string propertyName)
    {
        if (avatarFile is null || avatarFile.Length == 0)
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            var response = _fileSettingServices.UploadImage(TeacherAvatarUploadFolder, avatarFile, Guid.NewGuid().ToString("N"));

            if (!response.Success)
            {
                var errorMessage = response.Messages.FirstOrDefault()?.message ?? "ذخیره‌سازی تصویر انجام نشد.";
                ModelState.AddModelError(propertyName, errorMessage);
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult(response.Data);
        }
        catch
        {
            ModelState.AddModelError(propertyName, "ذخیره‌سازی تصویر انجام نشد.");
            return Task.FromResult<string?>(null);
        }
    }

    private void DeleteAvatarFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        _fileSettingServices.DeleteFile(relativePath);
    }
}
