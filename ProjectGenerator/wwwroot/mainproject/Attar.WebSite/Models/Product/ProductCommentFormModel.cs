using System;
using System.ComponentModel.DataAnnotations;

namespace Attar.WebSite.Models.Product;

public class ProductCommentFormModel
{
    public Guid ProductId { get; set; }

    public Guid? ParentId { get; set; }

    [Required(ErrorMessage = "لطفاً نام خود را وارد کنید.")]
    [StringLength(200, ErrorMessage = "نام نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.")]
    [Display(Name = "نام و نام خانوادگی")]
    public string AuthorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "متن نظر را بنویسید.")]
    [StringLength(2000, ErrorMessage = "متن نظر نمی‌تواند بیشتر از ۲۰۰۰ کاراکتر باشد.")]
    [Display(Name = "متن نظر")]
    public string Content { get; set; } = string.Empty;

    [Range(0, 5, ErrorMessage = "امتیاز باید بین ۰ تا ۵ باشد.")]
    [Display(Name = "امتیاز")]
    public double Rating { get; set; } = 5;
}
