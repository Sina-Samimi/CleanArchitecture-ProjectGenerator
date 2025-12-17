using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogTableRenameTest.WebSite.Areas.Admin.Models;

public sealed record SellerProfileListItemViewModel(
    Guid Id,
    string DisplayName,
    string? LicenseNumber,
    DateOnly? LicenseIssueDate,
    DateOnly? LicenseExpiryDate,
    string? ShopAddress,
    string? WorkingHours,
    int? ExperienceYears,
    string? Bio,
    string? ContactEmail,
    string? ContactPhone,
    string? UserId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class SellerProfilesIndexViewModel
{
    public IReadOnlyCollection<SellerProfileListItemViewModel> Sellers { get; init; }
        = Array.Empty<SellerProfileListItemViewModel>();

    public int ActiveCount { get; init; }

    public int InactiveCount { get; init; }

    public string? SuccessMessage { get; init; }

    public string? ErrorMessage { get; init; }

    // Pagination
    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalPages { get; init; }

    public int TotalCount { get; init; }

    // Filters (persian date strings are kept so views can re-populate datepickers)
    public string? SelectedName { get; init; }

    public string? SelectedPhone { get; init; }

    public DateTimeOffset? SelectedDateFrom { get; init; }

    public DateTimeOffset? SelectedDateTo { get; init; }

    public string? SelectedDateFromPersian { get; init; }

    public string? SelectedDateToPersian { get; init; }
}

public sealed class SellerProfileFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "نام فروشنده")]
    [Required(ErrorMessage = "نام فروشنده را وارد کنید.")]
    [MaxLength(200, ErrorMessage = "نام نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "شماره مجوز")]
    [MaxLength(100, ErrorMessage = "شماره مجوز نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.")]
    public string? LicenseNumber { get; set; }

    [Display(Name = "تاریخ صدور مجوز")]
    [DataType(DataType.Date)]
    public DateOnly? LicenseIssueDate { get; set; }

    [Display(Name = "تاریخ انقضای مجوز")]
    [DataType(DataType.Date)]
    public DateOnly? LicenseExpiryDate { get; set; }

    [Display(Name = "آدرس فروشگاه")]
    [MaxLength(500, ErrorMessage = "آدرس فروشگاه نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")]
    public string? ShopAddress { get; set; }

    [Display(Name = "ساعات کاری")]
    [MaxLength(200, ErrorMessage = "ساعات کاری نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string? WorkingHours { get; set; }

    [Display(Name = "تجربه کاری (سال)")]
    [Range(0, 100, ErrorMessage = "تجربه کاری باید بین ۰ تا ۱۰۰ سال باشد.")]
    public int? ExperienceYears { get; set; }

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

    [Display(Name = "درصد فروش (اختیاری)")]
    [Range(0, 100, ErrorMessage = "درصد فروش باید بین ۰ تا ۱۰۰ باشد.")]
    public decimal? SellerSharePercentage { get; set; }

    [BindNever]
    public IReadOnlyCollection<SelectListItem> UserOptions { get; set; } = Array.Empty<SelectListItem>();
}

public sealed class SellerProfileViewModel
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? LicenseNumber { get; init; }
    public DateOnly? LicenseIssueDate { get; init; }
    public DateOnly? LicenseExpiryDate { get; init; }
    public string? ShopAddress { get; init; }
    public string? WorkingHours { get; init; }
    public int? ExperienceYears { get; init; }
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string? UserId { get; init; }
    public bool IsActive { get; init; }
    public decimal? SellerSharePercentage { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? UserFullName { get; init; }
    public string? UserEmail { get; init; }
    public string? UserPhoneNumber { get; init; }
    public int TotalProducts { get; init; }
    public int TotalOffers { get; init; }
    public decimal TotalSales { get; init; }
}