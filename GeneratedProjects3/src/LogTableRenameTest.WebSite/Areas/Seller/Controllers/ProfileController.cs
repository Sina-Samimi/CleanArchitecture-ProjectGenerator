using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Commands.Sellers;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Application.Queries.Sellers;
using LogTableRenameTest.SharedKernel.Authorization;
using LogTableRenameTest.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class ProfileController : Controller
{
    private const int MaxAvatarFileSizeKb = 2 * 1024;
    private static readonly string[] AllowedAvatarContentTypes =
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };
    private const string SellerAvatarUploadFolder = "seller-profiles";

    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;

    public ProfileController(IMediator mediator, IFormFileSettingServices fileSettingServices)
    {
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "پروفایل فروشنده";
        ViewData["Subtitle"] = "اطلاعات و مشخصات فروشنده";
        ViewData["Sidebar:ActiveTab"] = "profile";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var cancellationToken = HttpContext.RequestAborted;
        var query = new GetSellerProfileByUserIdQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Alert.Message"] = result.Error ?? "دریافت اطلاعات پروفایل با خطا مواجه شد.";
            TempData["Alert.Type"] = "danger";
            return RedirectToAction("Index", "Products");
        }

        var dto = result.Value;
        var viewModel = new SellerProfileViewModel
        {
            Id = dto.Id,
            DisplayName = dto.DisplayName,
            LicenseNumber = dto.LicenseNumber,
            LicenseIssueDate = dto.LicenseIssueDate,
            LicenseExpiryDate = dto.LicenseExpiryDate,
            ShopAddress = dto.ShopAddress,
            WorkingHours = dto.WorkingHours,
            ExperienceYears = dto.ExperienceYears,
            Bio = dto.Bio,
            AvatarUrl = dto.AvatarUrl,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        ViewData["Title"] = "ویرایش پروفایل فروشنده";
        ViewData["Subtitle"] = "به‌روزرسانی اطلاعات و مشخصات";
        ViewData["Sidebar:ActiveTab"] = "profile";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var cancellationToken = HttpContext.RequestAborted;
        var query = new GetSellerProfileByUserIdQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Alert.Message"] = result.Error ?? "دریافت اطلاعات پروفایل با خطا مواجه شد.";
            TempData["Alert.Type"] = "danger";
            return RedirectToAction("Index");
        }

        var dto = result.Value;
        var viewModel = new SellerProfileFormViewModel
        {
            Id = dto.Id,
            DisplayName = dto.DisplayName,
            LicenseNumber = dto.LicenseNumber,
            LicenseIssueDate = dto.LicenseIssueDate,
            LicenseExpiryDate = dto.LicenseExpiryDate,
            ShopAddress = dto.ShopAddress,
            WorkingHours = dto.WorkingHours,
            ExperienceYears = dto.ExperienceYears,
            Bio = dto.Bio,
            AvatarUrl = dto.AvatarUrl,
            OriginalAvatarUrl = dto.AvatarUrl,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SellerProfileFormViewModel model)
    {
        ViewData["Title"] = "ویرایش پروفایل فروشنده";
        ViewData["Subtitle"] = "به‌روزرسانی اطلاعات و مشخصات";
        ViewData["Sidebar:ActiveTab"] = "profile";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        NormalizeForm(model);

        var cancellationToken = HttpContext.RequestAborted;

        ValidateAvatar(model.AvatarFile, nameof(model.AvatarFile));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var uploadedAvatarPath = await SaveAvatarAsync(model.AvatarFile, nameof(model.AvatarFile));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var previousAvatarPath = model.OriginalAvatarUrl;
        var avatarPath = uploadedAvatarPath ?? model.AvatarUrl;

        // Get current seller profile to ensure user owns it
        var query = new GetSellerProfileByUserIdQuery(userId);
        var sellerResult = await _mediator.Send(query, cancellationToken);

        if (!sellerResult.IsSuccess || sellerResult.Value is null || sellerResult.Value.Id != model.Id)
        {
            if (!string.IsNullOrWhiteSpace(uploadedAvatarPath))
            {
                DeleteAvatarFile(uploadedAvatarPath);
            }
            TempData["Alert.Message"] = "شما اجازه ویرایش این پروفایل را ندارید.";
            TempData["Alert.Type"] = "danger";
            return RedirectToAction("Index");
        }

        var command = new UpdateSellerProfileCommand(
            model.Id,
            model.DisplayName,
            model.LicenseNumber,
            model.LicenseIssueDate,
            model.LicenseExpiryDate,
            model.ShopAddress,
            model.WorkingHours,
            model.ExperienceYears,
            model.Bio,
            avatarPath,
            model.ContactEmail,
            model.ContactPhone,
            userId,
            sellerResult.Value.IsActive); // Keep current IsActive status

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            if (!string.IsNullOrWhiteSpace(uploadedAvatarPath))
            {
                DeleteAvatarFile(uploadedAvatarPath);
            }

            ModelState.AddModelError(string.Empty, result.Error ?? "ویرایش پروفایل با خطا مواجه شد.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(uploadedAvatarPath) &&
            !string.IsNullOrWhiteSpace(previousAvatarPath) &&
            !string.Equals(previousAvatarPath, uploadedAvatarPath, StringComparison.OrdinalIgnoreCase))
        {
            DeleteAvatarFile(previousAvatarPath);
        }

        TempData["Alert.Message"] = "اطلاعات پروفایل با موفقیت به‌روزرسانی شد.";
        TempData["Alert.Type"] = "success";
        return RedirectToAction("Index");
    }

    private static void NormalizeForm(SellerProfileFormViewModel model)
    {
        if (model is null)
        {
            return;
        }

        model.DisplayName = model.DisplayName?.Trim() ?? string.Empty;
        model.LicenseNumber = string.IsNullOrWhiteSpace(model.LicenseNumber) ? null : model.LicenseNumber.Trim();
        model.ShopAddress = string.IsNullOrWhiteSpace(model.ShopAddress) ? null : model.ShopAddress.Trim();
        model.WorkingHours = string.IsNullOrWhiteSpace(model.WorkingHours) ? null : model.WorkingHours.Trim();
        model.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
        model.AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl.Trim();
        model.ContactEmail = string.IsNullOrWhiteSpace(model.ContactEmail) ? null : model.ContactEmail.Trim();
        model.ContactPhone = string.IsNullOrWhiteSpace(model.ContactPhone) ? null : model.ContactPhone.Trim();
        model.OriginalAvatarUrl = string.IsNullOrWhiteSpace(model.OriginalAvatarUrl) ? null : model.OriginalAvatarUrl.Trim();
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
        if (!Array.Exists(AllowedAvatarContentTypes, ct => string.Equals(ct, contentType, StringComparison.OrdinalIgnoreCase)))
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

        var response = _fileSettingServices.UploadImage(SellerAvatarUploadFolder, avatarFile, Guid.NewGuid().ToString("N"));
        if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
        {
            ModelState.AddModelError(propertyName, response.Messages.FirstOrDefault()?.message ?? "امکان ذخیره‌سازی تصویر وجود ندارد.");
            return Task.FromResult<string?>(null);
        }

        var normalizedPath = response.Data.Replace("\\", "/", StringComparison.Ordinal);
        if (!normalizedPath.StartsWith('/'))
        {
            normalizedPath = "/" + normalizedPath;
        }

        return Task.FromResult<string?>(normalizedPath);
    }

    private void DeleteAvatarFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _fileSettingServices.DeleteFile(path);
    }
}

