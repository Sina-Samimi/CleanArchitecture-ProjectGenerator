using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TestAttarClone.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TestAttarClone.WebSite.Areas.Admin.Models;

public sealed record BlogListItemViewModel(
    Guid Id,
    string Title,
    string Category,
    Guid CategoryId,
    string Author,
    Guid AuthorId,
    BlogStatus Status,
    DateTimeOffset? PublishedAt,
    int ReadingTimeMinutes,
    int LikeCount,
    int DislikeCount,
    int CommentCount,
    int ViewCount,
    DateTimeOffset UpdatedAt,
    string? FeaturedImagePath,
    string Robots,
    IReadOnlyCollection<string> Tags);

public sealed record BlogStatisticsViewModel(
    int Total,
    int Published,
    int Draft,
    int Trash,
    int TotalLikes,
    int TotalDislikes,
    int TotalViews,
    double AverageReadingTimeMinutes);

public sealed class BlogIndexFilterViewModel
{
    public string? Search { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? AuthorId { get; set; }

    public BlogStatus? Status { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? FromDate { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? ToDate { get; set; }
}

public sealed class BlogIndexRequest
{
    public string? Search { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? AuthorId { get; set; }

    public BlogStatus? Status { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class BlogIndexViewModel
{
    public IReadOnlyCollection<BlogListItemViewModel> Blogs { get; init; }
        = Array.Empty<BlogListItemViewModel>();

    public BlogStatisticsViewModel Statistics { get; init; }
        = new(0, 0, 0, 0, 0, 0, 0, 0);

    public BlogIndexFilterViewModel Filters { get; init; } = new();

    public IReadOnlyCollection<SelectListItem> CategoryOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> AuthorOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> StatusOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public int TotalCount { get; init; }

    public int FilteredCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalPages { get; init; }

    public int FirstItemIndex { get; init; }

    public int LastItemIndex { get; init; }

}

public sealed class BlogCategoriesViewModel
{
    public IReadOnlyCollection<BlogCategoryTreeItemViewModel> Categories { get; init; }
        = Array.Empty<BlogCategoryTreeItemViewModel>();

    public IReadOnlyCollection<SelectListItem> ParentOptions { get; init; }
        = Array.Empty<SelectListItem>();
}

public sealed record BlogAuthorListItemViewModel(
    Guid Id,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    bool IsActive,
    string? UserId,
    string? UserFullName,
    string? UserEmail,
    string? UserPhoneNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record BlogAuthorUserOptionViewModel(
    string UserId,
    string DisplayName,
    string? Email,
    string? PhoneNumber,
    bool IsAssigned);

public sealed class BlogAuthorsViewModel
{
    public IReadOnlyCollection<BlogAuthorListItemViewModel> Authors { get; init; }
        = Array.Empty<BlogAuthorListItemViewModel>();

    public IReadOnlyCollection<BlogAuthorUserOptionViewModel> UserOptions { get; init; }
        = Array.Empty<BlogAuthorUserOptionViewModel>();

    public int TotalCount { get; init; }

    public int ActiveCount { get; init; }

    public int InactiveCount { get; init; }
}

public sealed class BlogCommentItemViewModel
{
    public Guid Id { get; init; }

    public Guid? ParentId { get; init; }

    public string AuthorName { get; init; } = string.Empty;

    public string? AuthorEmail { get; init; }

    public string Content { get; init; } = string.Empty;

    public bool IsApproved { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string? ApprovedByName { get; init; }

    public DateTimeOffset? ApprovedAt { get; init; }

    public string? ParentAuthorName { get; init; }

    public string? ParentExcerpt { get; init; }
}

public sealed class BlogCommentListViewModel
{
    public Guid BlogId { get; init; }

    public string BlogTitle { get; init; } = string.Empty;

    public string BlogSlug { get; init; } = string.Empty;

    public int TotalCount { get; init; }

    public int ApprovedCount { get; init; }

    public int PendingCount { get; init; }

    public IReadOnlyCollection<BlogCommentItemViewModel> Comments { get; init; }
        = Array.Empty<BlogCommentItemViewModel>();
}

public sealed class BlogCategoryTreeItemViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Slug { get; init; }

    public string? Description { get; init; }

    public Guid? ParentId { get; init; }

    public int Depth { get; init; }

    public IReadOnlyCollection<BlogCategoryTreeItemViewModel> Children { get; init; }
        = Array.Empty<BlogCategoryTreeItemViewModel>();

    public IReadOnlyCollection<Guid> DescendantIds { get; init; }
        = Array.Empty<Guid>();
}

public class BlogCategoryFormModel
{
    [Display(Name = "نام دسته‌بندی")]
    [Required(ErrorMessage = "نام دسته‌بندی را وارد کنید.")]
    [MaxLength(200, ErrorMessage = "نام نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Slug")]
    [MaxLength(200, ErrorMessage = "Slug نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string? Slug { get; set; }

    [Display(Name = "توضیحات")]
    [MaxLength(500, ErrorMessage = "توضیحات نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")]
    public string? Description { get; set; }

    [Display(Name = "دسته‌بندی والد")]
    public Guid? ParentId { get; set; }
}

public sealed class BlogCategoryUpdateFormModel : BlogCategoryFormModel
{
    [Required(ErrorMessage = "شناسه دسته‌بندی معتبر نیست.")]
    public Guid Id { get; set; }
}

public class BlogAuthorFormModel
{
    [Display(Name = "نام نمایشی")]
    [Required(ErrorMessage = "نام نمایشی را وارد کنید.")]
    [MaxLength(200, ErrorMessage = "نام نمایشی نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "معرفی کوتاه")]
    [MaxLength(1000, ErrorMessage = "معرفی نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.")]
    public string? Bio { get; set; }

    [Display(Name = "آدرس تصویر")]
    [MaxLength(500, ErrorMessage = "آدرس تصویر نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")]
    [DataType(DataType.Url)]
    public string? AvatarUrl { get; set; }

    [Display(Name = "کاربر سیستم")]
    public string? UserId { get; set; }

    [Display(Name = "فعال")]
    public bool IsActive { get; set; } = true;
}

public sealed class BlogAuthorUpdateFormModel : BlogAuthorFormModel
{
    [Required(ErrorMessage = "شناسه نویسنده معتبر نیست.")]
    public Guid Id { get; set; }
}

public sealed class BlogFormSelectionsViewModel
{
    public IReadOnlyCollection<SelectListItem> CategoryOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> AuthorOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> StatusOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> RobotsOptions { get; init; }
        = Array.Empty<SelectListItem>();
}

public sealed class BlogFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "عنوان بلاگ")]
    [Required(ErrorMessage = "عنوان بلاگ را وارد کنید.")]
    [MaxLength(300, ErrorMessage = "عنوان نمی‌تواند بیش از ۳۰۰ کاراکتر باشد.")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "خلاصه")]
    [MaxLength(600, ErrorMessage = "خلاصه نمی‌تواند بیش از ۶۰۰ کاراکتر باشد.")]
    public string Summary { get; set; } = string.Empty;

    [Display(Name = "محتوا")]
    [Required(ErrorMessage = "محتوای بلاگ را وارد کنید.")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "دسته‌بندی")]
    [Required(ErrorMessage = "دسته‌بندی را انتخاب کنید.")]
    public Guid? CategoryId { get; set; }

    [Display(Name = "نویسنده")]
    [Required(ErrorMessage = "نویسنده را انتخاب کنید.")]
    public Guid? AuthorId { get; set; }

    [Display(Name = "زمان مطالعه (دقیقه)")]
    [Range(1, 600, ErrorMessage = "زمان مطالعه باید بین ۱ تا ۶۰۰ دقیقه باشد.")]
    public int ReadingTimeMinutes { get; set; } = 5;

    [Display(Name = "وضعیت انتشار")]
    public BlogStatus Status { get; set; } = BlogStatus.Draft;

    [Display(Name = "تاریخ انتشار")]
    [DataType(DataType.DateTime)]
    public DateTimeOffset? PublishedAt { get; set; }

    [Display(Name = "تاریخ انتشار (شمسی)")]
    public string? PublishedAtPersian { get; set; }

    [Display(Name = "ساعت انتشار")]
    public string? PublishedAtTime { get; set; }

    [Display(Name = "عنوان سئو")]
    public string SeoTitle { get; set; } = string.Empty;

    [Display(Name = "توضیحات سئو")]
    public string SeoDescription { get; set; } = string.Empty;

    [Display(Name = "کلمات کلیدی")]
    public string SeoKeywords { get; set; } = string.Empty;

    [Display(Name = "مسیر دسترسی (Slug)")]
    public string SeoSlug { get; set; } = string.Empty;

    [Display(Name = "عکس شاخص")]
    [DataType(DataType.Upload)]
    public IFormFile? FeaturedImage { get; set; }

    public string? FeaturedImagePath { get; set; }

    [Display(Name = "حذف تصویر فعلی")]
    public bool RemoveFeaturedImage { get; set; }

    [Display(Name = "دستورالعمل ربات‌ها")]
    public string Robots { get; set; } = string.Empty;

    [Display(Name = "برچسب‌ها")]
    [MaxLength(1000, ErrorMessage = "برچسب‌ها نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.")]
    public string Tags { get; set; } = string.Empty;

    public IReadOnlyCollection<string> TagItems { get; set; }
        = Array.Empty<string>();

    public BlogFormSelectionsViewModel Selections { get; set; }
        = new();
}
