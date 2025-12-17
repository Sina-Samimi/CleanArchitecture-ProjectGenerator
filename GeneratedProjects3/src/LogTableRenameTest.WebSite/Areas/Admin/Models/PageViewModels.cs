using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LogTableRenameTest.Application.DTOs.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogTableRenameTest.WebSite.Areas.Admin.Models;

public sealed record PageListItemViewModel(
    Guid Id,
    string Title,
    string Slug,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    int ViewCount,
    DateTimeOffset CreateDate);

public sealed class PageFormSelectionsViewModel
{
    public IReadOnlyCollection<SelectListItem> RobotsOptions { get; init; }
        = Array.Empty<SelectListItem>();
}

public sealed record PageFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "عنوان صفحه الزامی است")]
    [Display(Name = "عنوان")]
    [StringLength(300, ErrorMessage = "عنوان نمی‌تواند بیشتر از 300 کاراکتر باشد")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "آدرس صفحه الزامی است")]
    [Display(Name = "آدرس (Slug)")]
    [StringLength(250, ErrorMessage = "آدرس نمی‌تواند بیشتر از 250 کاراکتر باشد")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "آدرس فقط می‌تواند شامل حروف کوچک انگلیسی، اعداد و خط تیره باشد")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "محتوای صفحه الزامی است")]
    [Display(Name = "محتوا")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "عنوان SEO")]
    [StringLength(200, ErrorMessage = "عنوان SEO نمی‌تواند بیشتر از 200 کاراکتر باشد")]
    public string? MetaTitle { get; set; }

    [Display(Name = "توضیحات SEO")]
    [StringLength(500, ErrorMessage = "توضیحات SEO نمی‌تواند بیشتر از 500 کاراکتر باشد")]
    public string? MetaDescription { get; set; }

    [Display(Name = "کلمات کلیدی SEO")]
    [StringLength(400, ErrorMessage = "کلمات کلیدی SEO نمی‌تواند بیشتر از 400 کاراکتر باشد")]
    public string? MetaKeywords { get; set; }

    [Display(Name = "Robots")]
    [StringLength(100, ErrorMessage = "Robots نمی‌تواند بیشتر از 100 کاراکتر باشد")]
    public string? MetaRobots { get; set; }

    [Display(Name = "منتشر شده")]
    public bool IsPublished { get; set; }

    [Display(Name = "عکس شاخص")]
    [DataType(DataType.Upload)]
    public IFormFile? FeaturedImage { get; set; }

    public string? FeaturedImagePath { get; set; }

    [Display(Name = "حذف تصویر فعلی")]
    public bool RemoveFeaturedImage { get; set; }

    [Display(Name = "نمایش در فوتر")]
    public bool ShowInFooter { get; set; }

    [Display(Name = "نمایش در دسترسی سریع")]
    public bool ShowInQuickAccess { get; set; }

    public PageFormSelectionsViewModel Selections { get; set; } = new();
}

public sealed record PagesIndexViewModel
{
    public IReadOnlyCollection<PageListItemViewModel> Pages { get; init; } = Array.Empty<PageListItemViewModel>();
    public int TotalCount { get; init; }
    public bool? PublishedFilter { get; init; }
    public Application.DTOs.Pages.PageStatisticsDto? Statistics { get; init; }
}

