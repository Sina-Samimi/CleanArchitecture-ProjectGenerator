using System;
using System.ComponentModel.DataAnnotations;

namespace Attar.WebSite.Models.Product;

public class ProductCustomRequestFormModel
{
    [Display(Name = "نام و نام خانوادگی")]
    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است.")]
    [MaxLength(200, ErrorMessage = "نام و نام خانوادگی نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "شماره تماس")]
    [Required(ErrorMessage = "شماره تماس الزامی است.")]
    [MaxLength(50, ErrorMessage = "شماره تماس نمی‌تواند بیش از ۵۰ کاراکتر باشد.")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "ایمیل (اختیاری)")]
    [EmailAddress(ErrorMessage = "ایمیل وارد شده معتبر نیست.")]
    [MaxLength(256, ErrorMessage = "ایمیل نمی‌تواند بیش از ۲۵۶ کاراکتر باشد.")]
    public string? Email { get; set; }

    [Display(Name = "توضیحات و نیازمندی‌های شما")]
    [MaxLength(2000, ErrorMessage = "توضیحات نمی‌تواند بیش از ۲۰۰۰ کاراکتر باشد.")]
    public string? Message { get; set; }

    public Guid ProductId { get; set; }
}

