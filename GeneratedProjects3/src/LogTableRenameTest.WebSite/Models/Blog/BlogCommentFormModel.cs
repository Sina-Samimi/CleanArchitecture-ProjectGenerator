using System;
using System.ComponentModel.DataAnnotations;

namespace LogTableRenameTest.WebSite.Models.Blog;

public class BlogCommentFormModel
{
    public Guid BlogId { get; set; }

    public Guid? ParentId { get; set; }

    [Display(Name = "نام و نام خانوادگی")]
    [Required(ErrorMessage = "لطفاً نام خود را وارد کنید.")]
    [StringLength(100, ErrorMessage = "نام حداکثر می‌تواند {1} کاراکتر باشد.")]
    public string AuthorName { get; set; } = string.Empty;

    [Display(Name = "ایمیل")]
    [EmailAddress(ErrorMessage = "ایمیل وارد شده معتبر نیست.")]
    [StringLength(150)]
    public string? AuthorEmail { get; set; }

    [Display(Name = "متن دیدگاه")]
    [Required(ErrorMessage = "لطفاً متن دیدگاه را بنویسید.")]
    [StringLength(2000, MinimumLength = 5, ErrorMessage = "دیدگاه باید حداقل {2} و حداکثر {1} کاراکتر باشد.")]
    public string Content { get; set; } = string.Empty;
}
