using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TestAttarClone.Application.DTOs.Identity;
using TestAttarClone.WebSite.Models.Account;
using Microsoft.AspNetCore.Http;

namespace TestAttarClone.WebSite.Areas.User.Models;

public sealed class UserSettingsViewModel
{
    public required ProfileSummaryViewModel Summary { get; init; }

    public required UpdateProfileInputModel UpdateProfile { get; init; }

    public IReadOnlyCollection<ActivityEntryDto> ActivityEntries { get; init; } = Array.Empty<ActivityEntryDto>();
}

public sealed class ProfileSummaryViewModel
{
    public string? AvatarPath { get; init; }

    public required string FullName { get; init; }

    public string? Email { get; init; }

    public required string PhoneNumber { get; init; }

    public DateTimeOffset CreatedOn { get; init; }

    public DateTimeOffset LastModifiedOn { get; init; }

    public int CompletionPercent { get; init; }
}

public sealed class UpdateProfileInputModel
{
    [Display(Name = "شناسه کاربر")]
    public string UserId { get; set; } = string.Empty;

    [Display(Name = "آواتار فعلی")]
    public string? AvatarPath { get; set; }

    [Display(Name = "تصویر جدید")]
    public IFormFile? Avatar { get; set; }

    [Required(ErrorMessage = "وارد کردن نام کامل الزامی است.")]
    [Display(Name = "نام کامل")]
    [StringLength(200, ErrorMessage = "نام کامل نمی‌تواند بیش از 200 کاراکتر باشد.")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "فرمت ایمیل معتبر نیست.")]
    [Display(Name = "ایمیل (اختیاری)")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "وارد کردن شماره تماس الزامی است.")]
    [Display(Name = "شماره تماس")]
    [IranianMobilePhone(ErrorMessage = "شماره موبایل باید یک شماره موبایل معتبر ایرانی باشد (مثال: 09123456789)")]
    public string PhoneNumber { get; set; } = string.Empty;
}
