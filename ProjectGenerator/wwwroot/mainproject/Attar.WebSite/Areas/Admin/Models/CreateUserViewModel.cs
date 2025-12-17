using System.ComponentModel.DataAnnotations;
using Attar.WebSite.Models.Account;
using Microsoft.AspNetCore.Http;

namespace Attar.WebSite.Areas.Admin.Models;

public sealed class CreateUserViewModel
{
    [Display(Name = "آواتار")]
    public IFormFile? Avatar { get; set; }

    [Required(ErrorMessage = "وارد کردن نام کامل الزامی است.")]
    [Display(Name = "نام کامل")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "وارد کردن شماره تماس الزامی است.")]
    [Display(Name = "شماره تماس")]
    [IranianMobilePhone(ErrorMessage = "شماره موبایل باید یک شماره موبایل معتبر ایرانی باشد (مثال: 09123456789)")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "رمز عبور")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "رمز عبور حداقل باید ۶ کاراکتر باشد.")]
    public string? Password { get; set; }

    [Display(Name = "نقش‌های کاربر")]
    public List<string> SelectedRoles { get; set; } = new();

    public IReadOnlyCollection<RoleOptionViewModel> AvailableRoles { get; set; } = Array.Empty<RoleOptionViewModel>();

    [Display(Name = "کاربر فعال باشد؟")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "دلیل غیرفعال‌سازی")]
    public string? DeactivationReason { get; set; }
}
