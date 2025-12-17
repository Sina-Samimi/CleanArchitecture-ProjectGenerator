using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Attar.Application.DTOs.Banners;

namespace Attar.WebSite.Areas.Admin.Models.Banners;

public sealed class BannerListViewModel
{
    public IReadOnlyCollection<BannerViewModel> Banners { get; init; } = Array.Empty<BannerViewModel>();

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public bool? IsActive { get; init; }

    public bool? ShowOnHomePage { get; init; }
}

public sealed class BannerViewModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;

    public string? LinkUrl { get; init; }

    public string? AltText { get; init; }

    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset? StartDate { get; init; }

    public DateTimeOffset? EndDate { get; init; }

    public bool ShowOnHomePage { get; init; }

    public DateTimeOffset CreateDate { get; init; }

    public DateTimeOffset UpdateDate { get; init; }
}

public sealed class BannerFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "عنوان بنر الزامی است.")]
    [StringLength(200, ErrorMessage = "عنوان بنر نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.")]
    [Display(Name = "عنوان")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "تصویر")]
    public Microsoft.AspNetCore.Http.IFormFile? Image { get; set; }

    public string? ImagePath { get; set; }

    public bool RemoveImage { get; set; }

    [StringLength(1000, ErrorMessage = "لینک نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد.")]
    [Display(Name = "لینک")]
    public string? LinkUrl { get; set; }

    [StringLength(200, ErrorMessage = "متن جایگزین نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.")]
    [Display(Name = "متن جایگزین")]
    public string? AltText { get; set; }

    [Display(Name = "ترتیب نمایش")]
    public int DisplayOrder { get; set; }

    [Display(Name = "فعال")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "تاریخ شروع")]
    public DateTimeOffset? StartDate { get; set; }

    [Display(Name = "تاریخ شروع (فارسی)")]
    public string? StartDatePersian { get; set; }

    [Display(Name = "تاریخ پایان")]
    public DateTimeOffset? EndDate { get; set; }

    [Display(Name = "تاریخ پایان (فارسی)")]
    public string? EndDatePersian { get; set; }

    [Display(Name = "نمایش در صفحه اصلی")]
    public bool ShowOnHomePage { get; set; } = true;
}

