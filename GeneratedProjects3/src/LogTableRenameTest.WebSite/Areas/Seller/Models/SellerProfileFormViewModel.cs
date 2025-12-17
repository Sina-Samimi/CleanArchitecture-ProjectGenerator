using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LogTableRenameTest.WebSite.Areas.Seller.Models;

public sealed class SellerProfileFormViewModel
{
    public Guid Id { get; set; }

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
}

