using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EndPoint.WebSite.Areas.Admin.Models;

public sealed record TeacherProfileListItemViewModel(
    Guid Id,
    string DisplayName,
    string? Degree,
    string? Specialty,
    string? Bio,
    string? ContactEmail,
    string? ContactPhone,
    string? UserId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class TeacherProfilesIndexViewModel
{
    public IReadOnlyCollection<TeacherProfileListItemViewModel> Teachers { get; init; }
        = Array.Empty<TeacherProfileListItemViewModel>();

    public int ActiveCount { get; init; }

    public int InactiveCount { get; init; }

    public string? SuccessMessage { get; init; }

    public string? ErrorMessage { get; init; }
}

public sealed class TeacherProfileFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "نام مدرس")]
    [Required(ErrorMessage = "نام مدرس را وارد کنید.")]
    [MaxLength(200, ErrorMessage = "نام نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "مدرک تحصیلی")]
    [MaxLength(200, ErrorMessage = "مدرک تحصیلی نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string? Degree { get; set; }

    [Display(Name = "تخصص")]
    [MaxLength(200, ErrorMessage = "تخصص نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string? Specialty { get; set; }

    [Display(Name = "بیوگرافی")]
    [MaxLength(2000, ErrorMessage = "بیوگرافی نمی‌تواند بیش از ۲۰۰۰ کاراکتر باشد.")]
    public string? Bio { get; set; }

    [Display(Name = "تصویر معرفی")]
    [DataType(DataType.Upload)]
    public IFormFile? AvatarFile { get; set; }

    [Display(Name = "لینک تصویر معرفی")]
    [MaxLength(500, ErrorMessage = "آدرس تصویر نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")]
    public string? AvatarUrl { get; set; }

    [Display(Name = "تصویر فعلی")]
    public string? OriginalAvatarUrl { get; set; }

    [Display(Name = "ایمیل ارتباطی")]
    [EmailAddress(ErrorMessage = "ایمیل وارد شده معتبر نیست.")]
    [MaxLength(200, ErrorMessage = "ایمیل نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string? ContactEmail { get; set; }

    [Display(Name = "شماره تماس")]
    [MaxLength(50, ErrorMessage = "شماره تماس نمی‌تواند بیش از ۵۰ کاراکتر باشد.")]
    public string? ContactPhone { get; set; }

    [Display(Name = "شناسه کاربری سیستم")]
    [MaxLength(450, ErrorMessage = "شناسه کاربری نمی‌تواند بیش از ۴۵۰ کاراکتر باشد.")]
    public string? UserId { get; set; }

    [Display(Name = "فعال")]
    public bool IsActive { get; set; } = true;

    [BindNever]
    public IReadOnlyCollection<SelectListItem> UserOptions { get; set; } = Array.Empty<SelectListItem>();
}
