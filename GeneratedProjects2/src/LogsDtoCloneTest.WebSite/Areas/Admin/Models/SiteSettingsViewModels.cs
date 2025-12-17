using System.ComponentModel.DataAnnotations;
using LogsDtoCloneTest.Application.Commands.Admin.SiteSettings;
using LogsDtoCloneTest.Application.DTOs.Settings;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed class SiteSettingsViewModel
{
    [Display(Name = "عنوان سایت")]
    [Required(ErrorMessage = "عنوان سایت الزامی است.")]
    [StringLength(200, ErrorMessage = "عنوان سایت نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.")]
    public string SiteTitle { get; set; } = string.Empty;

    [Display(Name = "ایمیل اصلی سایت")]
    [EmailAddress(ErrorMessage = "ایمیل وارد شده معتبر نیست.")]
    [StringLength(256, ErrorMessage = "ایمیل نمی‌تواند بیشتر از ۲۵۶ کاراکتر باشد.")]
    public string SiteEmail { get; set; } = string.Empty;

    [Display(Name = "ایمیل پشتیبانی")]
    [EmailAddress(ErrorMessage = "ایمیل وارد شده معتبر نیست.")]
    [StringLength(256, ErrorMessage = "ایمیل نمی‌تواند بیشتر از ۲۵۶ کاراکتر باشد.")]
    public string SupportEmail { get; set; } = string.Empty;

    [Display(Name = "شماره تماس اصلی")]
    [StringLength(50, ErrorMessage = "شماره تماس نمی‌تواند بیشتر از ۵۰ کاراکتر باشد.")]
    public string ContactPhone { get; set; } = string.Empty;

    [Display(Name = "شماره تماس پشتیبانی")]
    [StringLength(50, ErrorMessage = "شماره تماس نمی‌تواند بیشتر از ۵۰ کاراکتر باشد.")]
    public string SupportPhone { get; set; } = string.Empty;

    [Display(Name = "آدرس")]
    [StringLength(500, ErrorMessage = "آدرس نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.")]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "توضیحات تماس / ساعات پاسخگویی")]
    [StringLength(1000, ErrorMessage = "توضیحات نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد.")]
    public string ContactDescription { get; set; } = string.Empty;

    [Display(Name = "لوگوی سایت")]
    public IFormFile? Logo { get; set; }

    public string? LogoPath { get; set; }

    public bool RemoveLogo { get; set; }

    [Display(Name = "Favicon")]
    public IFormFile? Favicon { get; set; }

    public string? FaviconPath { get; set; }

    public bool RemoveFavicon { get; set; }

    [Display(Name = "توضیح مختصر")]
    public string? ShortDescription { get; set; }

    [Display(Name = "شرایط و قوانین")]
    public string? TermsAndConditions { get; set; }


    [Display(Name = "لینک تلگرام")]
    [Url(ErrorMessage = "لینک تلگرام معتبر نیست.")]
    [StringLength(500, ErrorMessage = "لینک نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.")]
    public string? TelegramUrl { get; set; }

    [Display(Name = "لینک اینستاگرام")]
    [Url(ErrorMessage = "لینک اینستاگرام معتبر نیست.")]
    [StringLength(500, ErrorMessage = "لینک نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.")]
    public string? InstagramUrl { get; set; }

    [Display(Name = "لینک واتساپ")]
    [Url(ErrorMessage = "لینک واتساپ معتبر نیست.")]
    [StringLength(500, ErrorMessage = "لینک نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.")]
    public string? WhatsAppUrl { get; set; }

    [Display(Name = "لینک لینکدین")]
    [Url(ErrorMessage = "لینک لینکدین معتبر نیست.")]
    [StringLength(500, ErrorMessage = "لینک نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.")]
    public string? LinkedInUrl { get; set; }

    [Display(Name = "نمایش بنرها به صورت اسلایدر")]
    public bool BannersAsSlider { get; set; }

    public static SiteSettingsViewModel FromDto(SiteSettingDto dto)
        => new()
        {
            SiteTitle = dto.SiteTitle,
            SiteEmail = dto.SiteEmail,
            SupportEmail = dto.SupportEmail,
            ContactPhone = dto.ContactPhone,
            SupportPhone = dto.SupportPhone,
            Address = dto.Address,
            ContactDescription = dto.ContactDescription,
            LogoPath = dto.LogoPath,
            FaviconPath = dto.FaviconPath,
            ShortDescription = dto.ShortDescription,
            TermsAndConditions = dto.TermsAndConditions,
            TelegramUrl = dto.TelegramUrl,
            InstagramUrl = dto.InstagramUrl,
            WhatsAppUrl = dto.WhatsAppUrl,
            LinkedInUrl = dto.LinkedInUrl,
            BannersAsSlider = dto.BannersAsSlider
        };

    public UpdateSiteSettingsCommand ToCommand()
        => new(
            SiteTitle,
            SiteEmail,
            SupportEmail,
            ContactPhone,
            SupportPhone,
            Address,
            ContactDescription,
            LogoPath,
            FaviconPath,
            ShortDescription,
            TermsAndConditions,
            TelegramUrl,
            InstagramUrl,
            WhatsAppUrl,
            LinkedInUrl,
            BannersAsSlider);
}
