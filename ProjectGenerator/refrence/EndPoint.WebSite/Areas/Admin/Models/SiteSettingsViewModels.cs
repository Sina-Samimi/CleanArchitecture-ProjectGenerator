using System.ComponentModel.DataAnnotations;
using Arsis.Application.Commands.Admin.SiteSettings;
using Arsis.Application.DTOs.Settings;

namespace EndPoint.WebSite.Areas.Admin.Models;

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

    public static SiteSettingsViewModel FromDto(SiteSettingDto dto)
        => new()
        {
            SiteTitle = dto.SiteTitle,
            SiteEmail = dto.SiteEmail,
            SupportEmail = dto.SupportEmail,
            ContactPhone = dto.ContactPhone,
            SupportPhone = dto.SupportPhone,
            Address = dto.Address,
            ContactDescription = dto.ContactDescription
        };

    public UpdateSiteSettingsCommand ToCommand()
        => new(
            SiteTitle,
            SiteEmail,
            SupportEmail,
            ContactPhone,
            SupportPhone,
            Address,
            ContactDescription);
}
