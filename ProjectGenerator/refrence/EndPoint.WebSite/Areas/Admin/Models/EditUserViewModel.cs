using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EndPoint.WebSite.Areas.Admin.Models;

public sealed class EditUserViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Display(Name = "آواتار")]
    public string? AvatarPath { get; set; }

    [Display(Name = "تصویر جدید")]
    public IFormFile? Avatar { get; set; }

    [Required(ErrorMessage = "وارد کردن ایمیل الزامی است.")]
    [Display(Name = "ایمیل")]
    [EmailAddress(ErrorMessage = "فرمت ایمیل معتبر نیست.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "وارد کردن نام کامل الزامی است.")]
    [Display(Name = "نام کامل")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "نقش‌های کاربر")]
    public List<string> SelectedRoles { get; set; } = new();

    public IReadOnlyCollection<RoleOptionViewModel> AvailableRoles { get; set; } = Array.Empty<RoleOptionViewModel>();

    [Display(Name = "کاربر فعال باشد؟")]
    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    [Required(ErrorMessage = "وارد کردن شماره تماس الزامی است.")]
    [Display(Name = "شماره تماس")]
    public string PhoneNumber { get; set; } = string.Empty;
}
