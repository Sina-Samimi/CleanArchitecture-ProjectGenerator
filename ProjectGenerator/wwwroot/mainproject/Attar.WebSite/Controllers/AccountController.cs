using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Interfaces;
using Attar.Domain.Entities;
using Attar.SharedKernel.Authorization;
using Attar.WebSite.Models.Account;
using Attar.Application.Commands.Cart;
using Attar.WebSite.Services;
using Attar.WebSite.Services.Cart;
using Attar.WebSite.Services.Session;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Attar.SharedKernel.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Attar.WebSite.Controllers;

[AllowAnonymous]
public sealed class AccountController : Controller
{
    private const string PhoneVerificationInfoTempDataKey = "Account.PhoneVerification.Info";
    private const string PhoneVerificationErrorTempDataKey = "Account.PhoneVerification.Error";
    private const string PhoneVerificationPhoneTempDataKey = "Account.PhoneVerification.Phone";
    private const string PhoneLoginPrefillTempDataKey = "Account.PhoneLogin.Phone";
    private const string PhoneLoginErrorTempDataKey = "Account.PhoneLogin.Error";
    private const string PhoneVerificationDebugCodeTempDataKey = "Account.PhoneVerification.DebugCode";

    private static readonly int[] PhoneNumberGroups = { 4, 3, 4 };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPhoneVerificationService _phoneVerificationService;
    private readonly ISmsSender _smsSender;
    private readonly ILogger<AccountController> _logger;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IMediator _mediator;
    private readonly ICartCookieService _cartCookieService;
    private readonly ISessionCookieService _sessionCookieService;
    private readonly ISellerProfileRepository _sellerProfileRepository;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IPhoneVerificationService phoneVerificationService,
        ISmsSender smsSender,
        ILogger<AccountController> logger,
        IUserSessionRepository userSessionRepository,
        IMediator mediator,
        ICartCookieService cartCookieService,
        ISessionCookieService sessionCookieService,
        ISellerProfileRepository sellerProfileRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _phoneVerificationService = phoneVerificationService;
        _smsSender = smsSender;
        _logger = logger;
        _userSessionRepository = userSessionRepository;
        _mediator = mediator;
        _cartCookieService = cartCookieService;
        _sessionCookieService = sessionCookieService;
        _sellerProfileRepository = sellerProfileRepository;
    }

    [HttpGet]
    public IActionResult PhoneLogin(string? returnUrl = null, string? phone = null)
    {
        var normalizedReturnUrl = NormalizeReturnUrl(returnUrl);
        var phoneFromTempData = TempData.Peek(PhoneLoginPrefillTempDataKey) as string;
        var normalizedPhone = NormalizePhoneNumber(
            !string.IsNullOrWhiteSpace(phone)
                ? phone
                : phoneFromTempData);

        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            TempData[PhoneLoginPrefillTempDataKey] = normalizedPhone;
        }

        var model = new PhoneLoginViewModel
        {
            ReturnUrl = normalizedReturnUrl,
            NormalizedPhoneNumber = string.IsNullOrWhiteSpace(normalizedPhone) ? null : normalizedPhone
        };

        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            model.PhoneNumber = FormatPhoneNumberForDisplay(normalizedPhone, phone ?? normalizedPhone);
        }

        if (TempData.TryGetValue(PhoneLoginErrorTempDataKey, out var errorObj)
            && errorObj is string loginError
            && !string.IsNullOrWhiteSpace(loginError))
        {
            model.ErrorMessage = loginError;
        }

        return View("PhoneLogin", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendCode(PhoneLoginViewModel model, CancellationToken cancellationToken)
    {
        model.ReturnUrl = NormalizeReturnUrl(model.ReturnUrl);

        var normalizedPhone = NormalizePhoneNumber(model.PhoneNumber);
        
        // Validate Iranian mobile phone format
        if (string.IsNullOrWhiteSpace(normalizedPhone) || normalizedPhone.Length != 11 || !normalizedPhone.StartsWith("09", StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "لطفاً شماره موبایل معتبر ایرانی وارد کنید (مثال: 09123456789).");
        }

        model.PhoneNumber = FormatPhoneNumberForDisplay(normalizedPhone, model.PhoneNumber);
        model.NormalizedPhoneNumber = normalizedPhone;

        // Preserve phone number in TempData for error cases
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            TempData[PhoneLoginPrefillTempDataKey] = normalizedPhone;
        }

        if (!ModelState.IsValid)
        {
            model.CodeSent = false;
            model.CodeExpiresAt = null;
            model.InfoMessage = null;
            model.ErrorMessage = null;
            return View("PhoneLogin", model);
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(
            u => u.PhoneNumber == normalizedPhone,
            cancellationToken);

        if (user is not null && (user.IsDeleted || !user.IsActive))
        {
            var errorMessage = await GetBlockedUserMessageAsync(user, cancellationToken);
            model.ErrorMessage = errorMessage;
            TempData[PhoneLoginErrorTempDataKey] = errorMessage;
            TempData[PhoneLoginPrefillTempDataKey] = normalizedPhone; // Preserve phone number
            model.CodeSent = false;
            model.CodeExpiresAt = null;
            return View("PhoneLogin", model);
        }

        var desiredUserName = BuildPhoneUserName(normalizedPhone);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = desiredUserName,
                NormalizedUserName = _userManager.NormalizeName(desiredUserName),
                Email = null,
                PhoneNumber = normalizedPhone,
                PhoneNumberConfirmed = false,
                IsActive = true,
                CreatedOn = DateTimeOffset.UtcNow,
                LastModifiedOn = DateTimeOffset.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                TempData[PhoneLoginPrefillTempDataKey] = normalizedPhone; // Preserve phone number
                model.CodeSent = false;
                model.CodeExpiresAt = null;
                return View("PhoneLogin", model);
            }

            // Assign default "User" role to newly registered users
            var userRole = await _roleManager.FindByNameAsync(RoleNames.User);
            if (userRole is null)
            {
                userRole = new IdentityRole(RoleNames.User);
                var createRoleResult = await _roleManager.CreateAsync(userRole);
                if (!createRoleResult.Succeeded)
                {
                    _logger.LogWarning("Failed to create User role during user registration: {Errors}",
                        string.Join(", ", createRoleResult.Errors.Select(e => e.Description)));
                }
            }

            if (userRole is not null)
            {
                var addToRoleResult = await _userManager.AddToRoleAsync(user, RoleNames.User);
                if (!addToRoleResult.Succeeded)
                {
                    _logger.LogWarning("Failed to assign User role to newly registered user {UserId}: {Errors}",
                        user.Id, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                }
            }
        }
        else if (!string.Equals(user.UserName, desiredUserName, StringComparison.OrdinalIgnoreCase))
        {
            user.UserName = desiredUserName;
            user.NormalizedUserName = _userManager.NormalizeName(desiredUserName);
            var updateUserNameResult = await _userManager.UpdateAsync(user);
            if (!updateUserNameResult.Succeeded)
            {
                foreach (var error in updateUserNameResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                TempData[PhoneLoginPrefillTempDataKey] = normalizedPhone; // Preserve phone number
                model.CodeSent = false;
                model.CodeExpiresAt = null;
                return View("PhoneLogin", model);
            }
        }

        var issuance = _phoneVerificationService.GenerateCode(normalizedPhone);
        await _smsSender.SendVerificationCodeAsync(normalizedPhone, issuance.Code, _phoneVerificationService.CodeLifetime, cancellationToken);

        TempData[PhoneVerificationInfoTempDataKey] = "کد تایید برای شما ارسال شد.";

        TempData[PhoneVerificationPhoneTempDataKey] = normalizedPhone;
        TempData[PhoneLoginPrefillTempDataKey] = normalizedPhone;
        TempData[PhoneVerificationDebugCodeTempDataKey] = issuance.Code;

        return RedirectToAction(
            nameof(PhoneVerification),
            new
            {
                returnUrl = model.ReturnUrl
            });
    }

    [HttpGet]
    public IActionResult PhoneVerification(string? returnUrl = null, string? phone = null)
    {
        var normalizedReturnUrl = NormalizeReturnUrl(returnUrl);
        var rawPhone = !string.IsNullOrWhiteSpace(phone)
            ? phone
            : TempData.Peek(PhoneVerificationPhoneTempDataKey) as string;
        var normalizedPhone = NormalizePhoneNumber(rawPhone);

        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            TempData[PhoneVerificationPhoneTempDataKey] = normalizedPhone;
            TempData[PhoneLoginPrefillTempDataKey] = normalizedPhone;
        }
        else
        {
            TempData.Remove(PhoneVerificationPhoneTempDataKey);
        }

        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            TempData[PhoneLoginErrorTempDataKey] = "لطفاً شماره موبایل خود را وارد کنید.";
            TempData.Remove(PhoneVerificationDebugCodeTempDataKey);
            return RedirectToAction(nameof(PhoneLogin), new { returnUrl = normalizedReturnUrl });
        }

        var state = _phoneVerificationService.GetState(normalizedPhone);
        if (state is null)
        {
            TempData[PhoneLoginErrorTempDataKey] = "کدی برای این شماره یافت نشد. لطفاً دوباره درخواست ارسال کد را انجام دهید.";
            TempData[PhoneLoginPrefillTempDataKey] = normalizedPhone;
            TempData.Remove(PhoneVerificationDebugCodeTempDataKey);
            return RedirectToAction(nameof(PhoneLogin), new { returnUrl = normalizedReturnUrl });
        }

        var model = new PhoneLoginViewModel
        {
            ReturnUrl = normalizedReturnUrl,
            PhoneNumber = FormatPhoneNumberForDisplay(normalizedPhone, normalizedPhone),
            NormalizedPhoneNumber = normalizedPhone,
            CodeSent = true,
            CodeExpiresAt = state.ExpiresAt,
            AcceptTerms = true,
            VerificationCode = string.Empty
        };

        var debugCode = TempData.Peek(PhoneVerificationDebugCodeTempDataKey) as string;
        if (!string.IsNullOrWhiteSpace(debugCode))
        {
            ViewData["DebugVerificationCode"] = debugCode;
        }

        if (TempData.TryGetValue(PhoneVerificationInfoTempDataKey, out var infoObj)
            && infoObj is string infoMessage
            && !string.IsNullOrWhiteSpace(infoMessage))
        {
            model.InfoMessage = infoMessage;
        }

        if (TempData.TryGetValue(PhoneVerificationErrorTempDataKey, out var errorObj)
            && errorObj is string errorMessage
            && !string.IsNullOrWhiteSpace(errorMessage))
        {
            model.ErrorMessage = errorMessage;
        }

        return View("PhoneVerification", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyCode(PhoneLoginViewModel model, CancellationToken cancellationToken)
    {
        model.ReturnUrl = NormalizeReturnUrl(model.ReturnUrl);
        ModelState.Remove(nameof(model.AcceptTerms));
        ModelState.Remove(nameof(model.PhoneNumber));
        var normalizedPhone = NormalizePhoneNumber(
            !string.IsNullOrWhiteSpace(model.NormalizedPhoneNumber)
                ? model.NormalizedPhoneNumber
                : model.PhoneNumber);

        var debugCode = TempData.Peek(PhoneVerificationDebugCodeTempDataKey) as string;
        if (!string.IsNullOrWhiteSpace(debugCode))
        {
            ViewData["DebugVerificationCode"] = debugCode;
        }

        model.PhoneNumber = FormatPhoneNumberForDisplay(normalizedPhone, model.PhoneNumber);
        model.NormalizedPhoneNumber = normalizedPhone;
        model.CodeSent = true;

        if (string.IsNullOrWhiteSpace(normalizedPhone) || normalizedPhone.Length != 11 || !normalizedPhone.StartsWith("09", StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "لطفاً شماره موبایل معتبر ایرانی وارد کنید (مثال: 09123456789).");
        }

        var sanitizedCode = model.VerificationCode?.Trim();
        if (string.IsNullOrWhiteSpace(sanitizedCode))
        {
            ModelState.AddModelError(nameof(model.VerificationCode), "لطفاً کد تایید را وارد کنید.");
        }
        else
        {
            model.VerificationCode = sanitizedCode;
        }

        var state = !string.IsNullOrWhiteSpace(normalizedPhone)
            ? _phoneVerificationService.GetState(normalizedPhone)
            : null;

        model.CodeExpiresAt = state?.ExpiresAt;

        if (!ModelState.IsValid)
        {
            model.InfoMessage = null;
            model.ErrorMessage ??= "لطفاً خطاهای فرم را بررسی کنید.";
            return View("PhoneVerification", model);
        }

        var verificationResult = _phoneVerificationService.ValidateCode(normalizedPhone, sanitizedCode!);
        if (!verificationResult.Succeeded)
        {
            model.InfoMessage = null;
            switch (verificationResult.Error)
            {
                case PhoneVerificationError.Expired:
                    model.ErrorMessage = "کد وارد شده منقضی شده است. لطفاً مجدداً کد را دریافت کنید.";
                    model.CodeSent = true;
                    model.CodeExpiresAt = null;
                    break;
                case PhoneVerificationError.Incorrect:
                    model.ErrorMessage = "کد وارد شده صحیح نیست. لطفاً دوباره تلاش کنید.";
                    model.CodeExpiresAt = verificationResult.ExpiresAt;
                    break;
                default:
                    model.ErrorMessage = "کدی برای این شماره یافت نشد. لطفاً دوباره درخواست ارسال کد را انجام دهید.";
                    model.CodeSent = false;
                    model.CodeExpiresAt = null;
                    break;
            }

            if (verificationResult.Error == PhoneVerificationError.NotFound)
            {
                TempData[PhoneVerificationErrorTempDataKey] = model.ErrorMessage;
                TempData[PhoneVerificationPhoneTempDataKey] = normalizedPhone;
                TempData[PhoneLoginPrefillTempDataKey] = normalizedPhone;
                TempData.Remove(PhoneVerificationDebugCodeTempDataKey);
                return RedirectToAction(nameof(PhoneVerification), new { returnUrl = model.ReturnUrl });
            }

            return View("PhoneVerification", model);
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(
            u => u.PhoneNumber == normalizedPhone,
            cancellationToken);

        if (user is null)
        {
            model.ErrorMessage = "کاربر مربوط به این شماره یافت نشد. لطفاً دوباره درخواست ارسال کد را انجام دهید.";
            model.CodeSent = false;
            model.CodeExpiresAt = null;
            model.InfoMessage = null;
            return View("PhoneVerification", model);
        }

        if (user.IsDeleted || !user.IsActive)
        {
            var errorMessage = await GetBlockedUserMessageAsync(user, cancellationToken);
            model.ErrorMessage = errorMessage;
            TempData[PhoneVerificationErrorTempDataKey] = errorMessage;
            model.InfoMessage = null;
            model.CodeSent = false;
            model.CodeExpiresAt = null;
            return View("PhoneVerification", model);
        }

        if (!user.PhoneNumberConfirmed)
        {
            user.PhoneNumberConfirmed = true;
            user.LastModifiedOn = DateTimeOffset.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    _logger.LogWarning("Failed to update phone confirmation for user {UserId}: {Error}", user.Id, error.Description);
                }
            }
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        var anonymousCartId = _cartCookieService.GetAnonymousCartId();
        if (anonymousCartId is not null && anonymousCartId.Value != Guid.Empty)
        {
            try
            {
                var mergeResult = await _mediator.Send(new MergeCartCommand(user.Id, anonymousCartId), cancellationToken);
                if (!mergeResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to merge shopping cart for user {UserId}: {Error}", user.Id, mergeResult.Error);
                }
                else
                {
                    _cartCookieService.ClearAnonymousCartId();
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while merging cart for user {UserId}. Ignoring merge and clearing anonymous cart.", user.Id);
                _cartCookieService.ClearAnonymousCartId();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while merging shopping cart for user {UserId}", user.Id);
            }
        }

        var userAgent = Request.Headers["User-Agent"].ToString();
        var session = UserSession.Start(
            user.Id,
            HttpContext.Connection.RemoteIpAddress,
            DetectDeviceType(userAgent),
            DetectClientName(userAgent),
            userAgent);

        await _userSessionRepository.AddAsync(session, cancellationToken);
        
        // Store session ID in cookie for current session identification
        _sessionCookieService.SetCurrentSessionId(session.Id);
        _phoneVerificationService.ClearCode(normalizedPhone);
        TempData.Remove(PhoneVerificationPhoneTempDataKey);
        TempData.Remove(PhoneLoginPrefillTempDataKey);
        TempData.Remove(PhoneVerificationDebugCodeTempDataKey);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendCode(PhoneLoginViewModel model, CancellationToken cancellationToken)
    {
        model.ReturnUrl = NormalizeReturnUrl(model.ReturnUrl);
        ModelState.Remove(nameof(model.AcceptTerms));
        ModelState.Remove(nameof(model.PhoneNumber));
        var normalizedPhone = NormalizePhoneNumber(
            !string.IsNullOrWhiteSpace(model.NormalizedPhoneNumber)
                ? model.NormalizedPhoneNumber
                : model.PhoneNumber);

        var debugCode = TempData.Peek(PhoneVerificationDebugCodeTempDataKey) as string;
        if (!string.IsNullOrWhiteSpace(debugCode))
        {
            ViewData["DebugVerificationCode"] = debugCode;
        }

        model.PhoneNumber = FormatPhoneNumberForDisplay(normalizedPhone, model.PhoneNumber);
        model.NormalizedPhoneNumber = normalizedPhone;
        model.CodeSent = true;

        if (string.IsNullOrWhiteSpace(normalizedPhone) || normalizedPhone.Length != 11 || !normalizedPhone.StartsWith("09", StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "لطفاً شماره موبایل معتبر ایرانی وارد کنید (مثال: 09123456789).");
            TempData[PhoneLoginPrefillTempDataKey] = normalizedPhone; // Preserve phone number
            model.CodeSent = false;
            model.CodeExpiresAt = null;
            model.InfoMessage = null;
            return View("PhoneVerification", model);
        }

        var existingState = _phoneVerificationService.GetState(normalizedPhone);
        if (existingState is not null && existingState.ExpiresAt > DateTimeOffset.UtcNow)
        {
            model.CodeExpiresAt = existingState.ExpiresAt;
            model.ErrorMessage = "تا پایان زمان باقی‌مانده امکان ارسال مجدد وجود ندارد.";
            model.InfoMessage = null;
            return View("PhoneVerification", model);
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(
            u => u.PhoneNumber == normalizedPhone,
            cancellationToken);

        if (user is null)
        {
            model.ErrorMessage = "کاربری با این شماره یافت نشد. لطفاً ابتدا ثبت نام کنید.";
            model.CodeSent = false;
            model.CodeExpiresAt = null;
            model.InfoMessage = null;
            return View("PhoneVerification", model);
        }

        if (user.IsDeleted || !user.IsActive)
        {
            var errorMessage = await GetBlockedUserMessageAsync(user, cancellationToken);
            model.ErrorMessage = errorMessage;
            TempData[PhoneVerificationErrorTempDataKey] = errorMessage;
            model.CodeSent = false;
            model.CodeExpiresAt = null;
            model.InfoMessage = null;
            return View("PhoneVerification", model);
        }

        var issuance = _phoneVerificationService.GenerateCode(normalizedPhone);
        await _smsSender.SendVerificationCodeAsync(normalizedPhone, issuance.Code, _phoneVerificationService.CodeLifetime, cancellationToken);

        TempData[PhoneVerificationInfoTempDataKey] = "کد جدید برای شما ارسال شد.";
        TempData[PhoneVerificationPhoneTempDataKey] = normalizedPhone;
        TempData[PhoneLoginPrefillTempDataKey] = normalizedPhone;
        TempData[PhoneVerificationDebugCodeTempDataKey] = issuance.Code;

        return RedirectToAction(
            nameof(PhoneVerification),
            new
            {
                returnUrl = model.ReturnUrl
            });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();

        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("User {UserId} signed out.", userId);
        }
        else
        {
            _logger.LogInformation("A user signed out.");
        }

        returnUrl = NormalizeReturnUrl(returnUrl);
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private static string NormalizeReturnUrl(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl) ? returnUrl : string.Empty;

    private static string NormalizePhoneNumber(string? phoneNumber)
    {
        var digits = PhoneNumberHelper.ExtractDigits(phoneNumber);
        if (string.IsNullOrWhiteSpace(digits))
        {
            return string.Empty;
        }

        var normalized = digits;
        if (normalized.StartsWith("0098", StringComparison.Ordinal) && normalized.Length >= 5)
        {
            normalized = "0" + normalized[4..];
        }
        else if (normalized.StartsWith("98", StringComparison.Ordinal) && normalized.Length >= 3)
        {
            normalized = "0" + normalized[2..];
        }
        else if (normalized.StartsWith("9", StringComparison.Ordinal) && normalized.Length == 10)
        {
            normalized = string.Concat("0", normalized);
        }

        if (normalized.Length > 11 && normalized.StartsWith("0", StringComparison.Ordinal))
        {
            normalized = normalized[^11..];
        }

        return normalized.Length == 11 && normalized.StartsWith("09", StringComparison.Ordinal) ? normalized : string.Empty;
    }

    private static string FormatPhoneNumberForDisplay(string normalized, string fallback)
    {
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        var parts = new string[PhoneNumberGroups.Length];
        var offset = 0;

        for (var index = 0; index < PhoneNumberGroups.Length && offset < normalized.Length; index++)
        {
            var length = PhoneNumberGroups[index];
            var take = Math.Min(length, normalized.Length - offset);
            parts[index] = normalized.Substring(offset, take);
            offset += take;
        }

        var result = string.Join(' ', parts.Where(part => !string.IsNullOrEmpty(part)));
        return string.IsNullOrWhiteSpace(result) ? fallback : result;
    }

    private static string BuildPhoneUserName(string normalizedPhone)
        => string.IsNullOrWhiteSpace(normalizedPhone)
            ? string.Empty
            : string.Concat(normalizedPhone, "@gmail.com");

    private async Task<string> GetBlockedUserMessageAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var isSeller = await _userManager.IsInRoleAsync(user, RoleNames.Seller);
        
        if (user.IsDeleted)
        {
            if (isSeller)
            {
                return "حساب کاربری فروشنده شما حذف شده است. شما اجازه ورود ندارید. لطفاً با پشتیبانی تماس بگیرید.";
            }
            return "حساب کاربری شما حذف شده است. شما اجازه ورود ندارید. لطفاً با پشتیبانی تماس بگیرید.";
        }
        
        if (!user.IsActive)
        {
            if (isSeller)
            {
                var sellerProfile = await _sellerProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
                if (sellerProfile != null && sellerProfile.IsDeleted)
                {
                    return "پروفایل فروشنده شما حذف شده است. لطفاً با پشتیبانی تماس بگیرید.";
                }
                return "پروفایل فروشنده شما غیرفعال شده است. لطفاً با پشتیبانی تماس بگیرید.";
            }
            return "حساب کاربری شما غیرفعال شده است. لطفاً با پشتیبانی تماس بگیرید.";
        }
        
        return "شما اجازه ورود ندارید. لطفاً با پشتیبانی تماس بگیرید.";
    }

    private void ShowBlockedAlert(string message)
    {
        // مدال غیرفعال شده - پیام خطا از طریق ErrorMessage در View نمایش داده می‌شود
    }

    private static string DetectDeviceType(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Unknown";
        }

        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
        {
            return "Tablet";
        }

        if (userAgent.Contains("Mobi", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            return "Mobile";
        }

        return "Desktop";
    }

    private static string DetectClientName(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Unknown";
        }

        if (userAgent.Contains("OPR/", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Opera", StringComparison.OrdinalIgnoreCase))
        {
            return "Opera";
        }

        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
        {
            return "Microsoft Edge";
        }

        if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("CriOS", StringComparison.OrdinalIgnoreCase))
        {
            return "Chrome";
        }

        if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
        {
            return "Firefox";
        }

        if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase)
            && userAgent.Contains("Version/", StringComparison.OrdinalIgnoreCase))
        {
            return "Safari";
        }

        if (userAgent.Contains("MSIE", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Trident/", StringComparison.OrdinalIgnoreCase))
        {
            return "Internet Explorer";
        }

        return "Unknown";
    }
}
