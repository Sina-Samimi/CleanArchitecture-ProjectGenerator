using System;
using System.ComponentModel.DataAnnotations;

namespace TestAttarClone.WebSite.Models.Product;

public class ProductViolationReportFormModel
{
    [Display(Name = "موضوع تخلف")]
    [Required(ErrorMessage = "موضوع تخلف الزامی است.")]
    [MaxLength(200, ErrorMessage = "موضوع تخلف نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string? Subject { get; set; }

    [Display(Name = "پیام")]
    [Required(ErrorMessage = "پیام الزامی است.")]
    [MaxLength(2000, ErrorMessage = "پیام نمی‌تواند بیش از ۲۰۰۰ کاراکتر باشد.")]
    public string? Message { get; set; }

    [Display(Name = "شماره تماس")]
    [Required(ErrorMessage = "شماره تماس الزامی است.")]
    [MaxLength(20, ErrorMessage = "شماره تماس نمی‌تواند بیش از ۲۰ کاراکتر باشد.")]
    public string? ReporterPhone { get; set; }

    public Guid? ProductOfferId { get; set; }

    public string? SellerId { get; set; }
}

