using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Commands.Identity;
using LogsDtoCloneTest.Application.Commands.Identity.UpdateUser;
using LogsDtoCloneTest.Application.DTOs;
using LogsDtoCloneTest.Application.DTOs.Identity;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Application.Queries.Admin.SiteSettings;
using LogsDtoCloneTest.Application.Queries.Identity;
using LogsDtoCloneTest.Domain.Entities;
using LogsDtoCloneTest.SharedKernel.Extensions;
using LogsDtoCloneTest.SharedKernel.Helpers;
using LogsDtoCloneTest.WebSite.Areas.User.Models;
using LogsDtoCloneTest.WebSite.Services.Session;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LogsDtoCloneTest.WebSite.Areas.User.Controllers;

[Area("User")]
[Authorize]
public sealed class ProfileController : Controller
{
    private const int MaxAvatarFileSizeKb = 200;
    private const string AvatarUploadFolder = "users/profile";

    private static readonly HashSet<string> AllowedAvatarContentTypes = new(
        new[] { "image/png", "image/jpeg", "image/jpg", "image/webp" },
        StringComparer.OrdinalIgnoreCase);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IMediator _mediator;
    private readonly IFormFileSettingServices _fileSettingServices;
    private readonly ILogger<ProfileController> _logger;
    private readonly ISessionCookieService _sessionCookieService;
    private readonly IUserSessionRepository _userSessionRepository;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IMediator mediator,
        IFormFileSettingServices fileSettingServices,
        ILogger<ProfileController> logger,
        ISessionCookieService sessionCookieService,
        IUserSessionRepository userSessionRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _mediator = mediator;
        _fileSettingServices = fileSettingServices;
        _logger = logger;
        _sessionCookieService = sessionCookieService;
        _userSessionRepository = userSessionRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        // Get site settings for support information
        var siteSettingsResult = await _mediator.Send(new GetSiteSettingsQuery());
        if (siteSettingsResult.IsSuccess && siteSettingsResult.Value is not null)
        {
            ViewData["SupportPhone"] = siteSettingsResult.Value.SupportPhone;
            ViewData["SupportEmail"] = siteSettingsResult.Value.SupportEmail;
            ViewData["ContactPhone"] = siteSettingsResult.Value.ContactPhone;
            ViewData["ContactDescription"] = siteSettingsResult.Value.ContactDescription;
        }

        // Get recent devices (last 5 for dashboard) - we'll show devices instead of sessions
        var devicesQuery = new GetUserDevicesQuery(user.Id, PageNumber: 1, PageSize: 5);
        var devicesResult = await _mediator.Send(devicesQuery, HttpContext.RequestAborted);
        var recentDevices = devicesResult.IsSuccess ? devicesResult.Value.Items : Array.Empty<DeviceActivityDto>();
        
        // Convert to ActivityEntryDto for backward compatibility with existing view
        var activityEntries = recentDevices.Select(d => new ActivityEntryDto(
            SessionId: null,
            Title: d.IsActive ? "ورود به سیستم" : "خروج از سیستم",
            Timestamp: d.LastSeenAt,
            Context: $"{d.DeviceType} - {d.ClientName}",
            DeviceType: d.DeviceType,
            ClientName: d.ClientName,
            IpAddress: d.IpAddress,
            IsCurrentSession: false,
            IsActive: d.IsActive)).ToList();

        var viewModel = BuildViewModel(user, profileForm: null, activityEntries);
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
                ModelState.AddModelError(nameof(model.Avatar), $"حجم تصویر نباید بیشتر از {MaxAvatarFileSizeKb} کیلوبایت باشد.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.AvatarPath = user.AvatarPath;
            // Get activity log for view
            var currentSessionId = _sessionCookieService.GetCurrentSessionId();
            var activityQuery = new GetUserActivityLogQuery(user.Id, currentSessionId, PageNumber: 1, PageSize: 5);
            var activityResult = await _mediator.Send(activityQuery, HttpContext.RequestAborted);
            var activityEntries = activityResult.IsSuccess ? activityResult.Value.Items : Array.Empty<ActivityEntryDto>();
            var invalidViewModel = BuildViewModel(user, model, activityEntries);
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
                // Get activity log for view
                var currentSessionId = _sessionCookieService.GetCurrentSessionId();
                var activityQuery = new GetUserActivityLogQuery(user.Id, currentSessionId, PageNumber: 1, PageSize: 5);
                var activityResult = await _mediator.Send(activityQuery, HttpContext.RequestAborted);
                var activityEntries = activityResult.IsSuccess ? activityResult.Value.Items : Array.Empty<ActivityEntryDto>();
                var failedViewModel = BuildViewModel(user, model, activityEntries);
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
            // Get activity log for view
            var currentSessionId = _sessionCookieService.GetCurrentSessionId();
            var activityQuery = new GetUserActivityLogQuery(user.Id, currentSessionId, PageNumber: 1, PageSize: 5);
            var activityResult = await _mediator.Send(activityQuery, HttpContext.RequestAborted);
            var activityEntries = activityResult.IsSuccess ? activityResult.Value.Items : Array.Empty<ActivityEntryDto>();
            var errorViewModel = BuildViewModel(user, model, activityEntries);
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
        var digits = PhoneNumberHelper.ExtractDigits(value);
        if (string.IsNullOrWhiteSpace(digits))
        {
            return null;
        }

        var normalized = digits;
        if (normalized.StartsWith("0098", StringComparison.Ordinal))
        {
            normalized = normalized[4..];
        }

        if (normalized.StartsWith("98", StringComparison.Ordinal) && normalized.Length >= 12)
        {
            normalized = normalized[2..];
        }

        if (normalized.StartsWith("9", StringComparison.Ordinal) && normalized.Length == 10)
        {
            normalized = string.Concat("0", normalized);
        }

        if (normalized.Length > 11 && normalized.StartsWith("0", StringComparison.Ordinal))
        {
            normalized = normalized[^11..];
        }

        return normalized.Length == 11 && normalized.StartsWith("09", StringComparison.Ordinal) ? normalized : null;
    }

    [HttpGet]
    public async Task<IActionResult> ActivityLog(int page = 1, int pageSize = 20, string? deviceType = null, bool? isActive = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        ViewData["Title"] = "دستگاه‌های متصل";
        ViewData["Subtitle"] = "مدیریت و مشاهده دستگاه‌های متصل شما";
        ViewData["Sidebar:ActiveTab"] = "profile";

        // Get devices with pagination and filters
        var devicesQuery = new GetUserDevicesQuery(
            user.Id,
            page,
            pageSize,
            deviceType,
            isActive);
        var devicesResult = await _mediator.Send(devicesQuery, HttpContext.RequestAborted);
        
        if (!devicesResult.IsSuccess)
        {
            TempData["Error"] = devicesResult.Error ?? "خطا در دریافت دستگاه‌های متصل.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = new DeviceActivityViewModel
        {
            Devices = devicesResult.Value.Items,
            PageNumber = devicesResult.Value.PageNumber,
            PageSize = devicesResult.Value.PageSize,
            TotalCount = devicesResult.Value.TotalCount,
            TotalPages = devicesResult.Value.TotalPages,
            FilterDeviceType = deviceType,
            FilterIsActive = isActive
        };

        return View("DeviceActivity", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseDevice(string deviceKey)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(deviceKey))
        {
            TempData["Error"] = "کلید دستگاه معتبر نیست.";
            return RedirectToAction(nameof(ActivityLog));
        }

        // Check if this is the current device
        var currentSessionId = _sessionCookieService.GetCurrentSessionId();
        var isCurrentDevice = false;
        if (currentSessionId.HasValue)
        {
            var devicesQuery = new GetUserDevicesQuery(user.Id, PageNumber: 1, PageSize: 100);
            var devicesResult = await _mediator.Send(devicesQuery, HttpContext.RequestAborted);
            if (devicesResult.IsSuccess)
            {
                // Check if current session belongs to this device
                var sessions = await _userSessionRepository.GetByUserIdAsync(user.Id, HttpContext.RequestAborted);
                var currentSession = sessions.FirstOrDefault(s => s.Id == currentSessionId.Value);
                if (currentSession != null)
                {
                    var currentDeviceKey = $"{currentSession.DeviceType}|{currentSession.ClientName}";
                    isCurrentDevice = currentDeviceKey == deviceKey;
                }
            }
        }

        var command = new CloseUserDeviceCommand(user.Id, deviceKey);
        var result = await _mediator.Send(command, HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در بستن دستگاه.";
            return RedirectToAction(nameof(ActivityLog));
        }

        // If current device was closed, logout the user immediately
        if (isCurrentDevice)
        {
            _sessionCookieService.ClearCurrentSessionId();
            await _signInManager.SignOutAsync();
            TempData["Success"] = "دستگاه شما بسته شد و از سیستم خارج شدید.";
            return RedirectToAction("PhoneLogin", "Account", new { area = "" });
        }

        // For other devices, Security Stamp is updated which will invalidate those sessions
        // The middleware will check and logout those devices on their next request
        TempData["Success"] = $"دستگاه با موفقیت بسته شد. {result.Value} session بسته شد و دستگاه در درخواست بعدی از سیستم خارج خواهد شد.";
        return RedirectToAction(nameof(ActivityLog));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteClosedDevices()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var command = new DeleteUserClosedDevicesCommand(user.Id);
        var result = await _mediator.Send(command, HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "خطا در حذف تاریخچه دستگاه‌های بسته شده.";
        }
        else
        {
            var deletedCount = result.Value;
            if (deletedCount > 0)
            {
                TempData["Success"] = $"{deletedCount} session بسته شده از تاریخچه حذف شد.";
            }
            else
            {
                TempData["Info"] = "هیچ session بسته شده‌ای برای حذف وجود ندارد.";
            }
        }

        return RedirectToAction(nameof(ActivityLog));
    }

    private static UserSettingsViewModel BuildViewModel(
        ApplicationUser user,
        UpdateProfileInputModel? profileForm,
        IReadOnlyCollection<ActivityEntryDto>? activityEntries = null)
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
            UpdateProfile = profileForm,
            ActivityEntries = activityEntries ?? Array.Empty<ActivityEntryDto>()
        };
    }
}
