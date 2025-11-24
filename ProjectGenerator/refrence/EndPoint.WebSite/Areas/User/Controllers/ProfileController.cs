using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arsis.Application.Commands.Identity.UpdateUser;
using Arsis.Application.DTOs;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities;
using Arsis.SharedKernel.Extensions;
using EndPoint.WebSite.Areas.User.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EndPoint.WebSite.Areas.User.Controllers;

[Area("User")]
[Authorize]
public sealed class ProfileController : Controller
{
    private const int MaxAvatarFileSizeKb = 2 * 1024;
    private const string AvatarUploadFolder = "users/profile";

    private static readonly HashSet<string> AllowedAvatarContentTypes = new(
        new[] { "image/png", "image/jpeg", "image/jpg", "image/webp" },
        StringComparer.OrdinalIgnoreCase);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IMediator mediator,
        IFormFileSettingServices fileSettingServices,
        ILogger<ProfileController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var viewModel = BuildViewModel(user, profileForm: null);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([Bind(Prefix = "UpdateProfile")] UpdateProfileInputModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        model.UserId = user.Id;

        var trimmedFullName = model.FullName?.Trim() ?? string.Empty;
        var emailSubmitted = model.Email is not null;
        var normalizedEmail = model.Email?.Trim();

        if (!string.Equals(model.FullName, trimmedFullName, StringComparison.Ordinal))
        {
            model.FullName = trimmedFullName;
        }

        if (!string.Equals(model.Email, normalizedEmail, StringComparison.Ordinal))
        {
            ModelState.Remove(nameof(model.Email));
            ModelState.Remove($"UpdateProfile.{nameof(model.Email)}");
            model.Email = normalizedEmail;
        }

        var normalizedPhone = NormalizePhoneNumber(model.PhoneNumber);
        if (normalizedPhone is null)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "شماره تماس باید با 09 شروع شده و 11 رقم باشد.");
        }
        else if (!string.Equals(model.PhoneNumber, normalizedPhone, StringComparison.Ordinal))
        {
            model.PhoneNumber = normalizedPhone;
        }

        if (model.Avatar is not null && model.Avatar.Length > 0)
        {
            if (!AllowedAvatarContentTypes.Contains(model.Avatar.ContentType))
            {
                ModelState.AddModelError(nameof(model.Avatar), "فرمت تصویر انتخاب شده مجاز نیست.");
            }
            else if (!_fileSettingServices.IsFileSizeValid(model.Avatar, MaxAvatarFileSizeKb))
            {
                ModelState.AddModelError(nameof(model.Avatar), "حجم تصویر نباید بیشتر از 2 مگابایت باشد.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.AvatarPath = user.AvatarPath;
            var invalidViewModel = BuildViewModel(user, model);
            return View("Index", invalidViewModel);
        }

        string? avatarPath = user.AvatarPath;
        string? uploadedAvatarPath = null;
        string? previousAvatarPath = user.AvatarPath;
        if (model.Avatar is not null && model.Avatar.Length > 0)
        {
            var uploadResult = _fileSettingServices.UploadImage(AvatarUploadFolder, model.Avatar, $"user-{user.Id}");
            if (!uploadResult.Success || string.IsNullOrWhiteSpace(uploadResult.Data))
            {
                _logger.LogWarning("Uploading avatar for user {UserId} failed: {Message}", user.Id, uploadResult.Messages?.FirstOrDefault()?.message);
                ModelState.AddModelError(nameof(model.Avatar), uploadResult.Messages?.FirstOrDefault()?.message ?? "بارگذاری تصویر با خطا مواجه شد.");
                model.AvatarPath = user.AvatarPath;
                var failedViewModel = BuildViewModel(user, model);
                return View("Index", failedViewModel);
            }

            uploadedAvatarPath = uploadResult.Data;
            if (!string.Equals(user.AvatarPath, uploadedAvatarPath, StringComparison.OrdinalIgnoreCase))
            {
                avatarPath = uploadedAvatarPath;
            }
        }

        var updateDto = new UpdateUserDto(
            user.Id,
            emailSubmitted ? normalizedEmail ?? string.Empty : null,
            model.FullName,
            Roles: null,
            IsActive: null,
            AvatarPath: avatarPath,
            PhoneNumber: normalizedPhone);

        var result = await _mediator.Send(new UpdateUserCommand(updateDto));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "به‌روزرسانی پروفایل با خطا مواجه شد.");
            if (!string.IsNullOrWhiteSpace(uploadedAvatarPath))
            {
                _ = _fileSettingServices.DeleteFile(uploadedAvatarPath);
            }
            model.AvatarPath = avatarPath;
            var errorViewModel = BuildViewModel(user, model);
            return View("Index", errorViewModel);
        }

        var refreshedUser = await _userManager.FindByIdAsync(user.Id) ?? user;
        await _signInManager.RefreshSignInAsync(refreshedUser);

        if (!string.IsNullOrWhiteSpace(uploadedAvatarPath) && !string.IsNullOrWhiteSpace(previousAvatarPath) &&
            !string.Equals(uploadedAvatarPath, previousAvatarPath, StringComparison.OrdinalIgnoreCase))
        {
            _ = _fileSettingServices.DeleteFile(previousAvatarPath);
        }

        TempData["Success"] = "پروفایل شما با موفقیت به‌روزرسانی شد.";
        return RedirectToAction(nameof(Index));
    }

    private static string? NormalizePhoneNumber(string? value)
    {
        var digits = UserFilterFormatting.NormalizePhoneNumber(value);
        if (string.IsNullOrWhiteSpace(digits))
        {
            return null;
        }

        if (digits.StartsWith("0098", StringComparison.Ordinal))
        {
            digits = digits[4..];
        }

        if (digits.StartsWith("98", StringComparison.Ordinal) && digits.Length >= 12)
        {
            digits = digits[2..];
        }

        if (digits.StartsWith("9", StringComparison.Ordinal) && digits.Length == 10)
        {
            digits = string.Concat("0", digits);
        }

        if (digits.Length > 11 && digits.StartsWith("0", StringComparison.Ordinal))
        {
            digits = digits[^11..];
        }

        return digits.Length == 11 && digits.StartsWith("09", StringComparison.Ordinal) ? digits : null;
    }

    private static UserSettingsViewModel BuildViewModel(
        ApplicationUser user,
        UpdateProfileInputModel? profileForm)
    {
        profileForm ??= new UpdateProfileInputModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? string.Empty
        };

        profileForm.UserId = user.Id;
        profileForm.AvatarPath ??= user.AvatarPath;
        profileForm.Avatar = null;

        var completionTotal = 4d;
        var completionScore = 0d;
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            completionScore++;
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            completionScore++;
        }

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            completionScore++;
        }

        if (!string.IsNullOrWhiteSpace(user.AvatarPath))
        {
            completionScore++;
        }

        var completionPercent = (int)Math.Round((completionScore / completionTotal) * 100d, MidpointRounding.AwayFromZero);

        return new UserSettingsViewModel
        {
            Summary = new ProfileSummaryViewModel
            {
                AvatarPath = user.AvatarPath,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                CreatedOn = user.CreatedOn,
                LastModifiedOn = user.LastModifiedOn,
                CompletionPercent = Math.Clamp(completionPercent, 0, 100)
            },
            UpdateProfile = profileForm
        };
    }
}
