using System.ComponentModel.DataAnnotations;

namespace TestAttarClone.WebSite.Models;

public sealed class ContactFormModel
{
    [Display(Name = "نام و نام خانوادگی")]
    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است.")]
    [StringLength(200, ErrorMessage = "نام و نام خانوادگی نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "ایمیل")]
    [Required(ErrorMessage = "ایمیل الزامی است.")]
    [EmailAddress(ErrorMessage = "ایمیل وارد شده معتبر نیست.")]
    [StringLength(256, ErrorMessage = "ایمیل نمی‌تواند بیشتر از ۲۵۶ کاراکتر باشد.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "شماره تماس")]
    [Required(ErrorMessage = "شماره تماس الزامی است.")]
    [StringLength(50, ErrorMessage = "شماره تماس نمی‌تواند بیشتر از ۵۰ کاراکتر باشد.")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "موضوع")]
    [Required(ErrorMessage = "موضوع الزامی است.")]
    [StringLength(500, ErrorMessage = "موضوع نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.")]
    public string Subject { get; set; } = string.Empty;

    [Display(Name = "پیام")]
    [Required(ErrorMessage = "پیام الزامی است.")]
    public string Message { get; set; } = string.Empty;
}

