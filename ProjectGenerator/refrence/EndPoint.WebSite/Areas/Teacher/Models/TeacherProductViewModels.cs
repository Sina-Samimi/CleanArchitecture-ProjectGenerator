using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Arsis.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EndPoint.WebSite.Areas.Teacher.Models;

public sealed record TeacherProductListItemViewModel(
    Guid Id,
    string Name,
    string Category,
    ProductType Type,
    decimal Price,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdatedAt,
    string? FeaturedImagePath,
    IReadOnlyCollection<string> Tags);

public sealed class TeacherProductIndexViewModel
{
    public IReadOnlyCollection<TeacherProductListItemViewModel> Products { get; init; }
        = Array.Empty<TeacherProductListItemViewModel>();

    public int TotalCount { get; init; }

    public int PendingCount { get; init; }

    public int PublishedCount { get; init; }

    public string? SuccessMessage { get; init; }

    public string? ErrorMessage { get; init; }

    public TeacherProductFilterViewModel Filter { get; init; } = new();

    public bool HasActiveFilters => Filter.HasValues;
}

public sealed class TeacherProductFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "نام محصول")]
    [Required(ErrorMessage = "نام محصول الزامی است.")]
    [MaxLength(200, ErrorMessage = "نام محصول نباید بیش از ۲۰۰ کاراکتر باشد.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "خلاصه کوتاه")]
    [MaxLength(500, ErrorMessage = "خلاصه محصول نباید بیش از ۵۰۰ کاراکتر باشد.")]
    public string? Summary { get; set; }

    [Display(Name = "توضیحات کامل")]
    [Required(ErrorMessage = "توضیحات محصول را وارد کنید.")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "نوع محصول")]
    [Required(ErrorMessage = "نوع محصول را انتخاب کنید.")]
    public ProductType Type { get; set; } = ProductType.Digital;

    [Display(Name = "قیمت (تومان)")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "قیمت محصول معتبر نیست.")]
    public decimal Price { get; set; }

    [Display(Name = "مدیریت موجودی")]
    public bool TrackInventory { get; set; }

    [Display(Name = "تعداد موجودی")]
    [Range(0, 1000000, ErrorMessage = "موجودی نمی‌تواند کمتر از صفر باشد.")]
    public int StockQuantity { get; set; }

    [Display(Name = "دسته‌بندی")]
    [Required(ErrorMessage = "انتخاب دسته‌بندی الزامی است.")]
    public Guid? CategoryId { get; set; }

    [Display(Name = "برچسب‌ها")]
    [MaxLength(600, ErrorMessage = "برچسب‌ها نباید بیش از ۶۰۰ کاراکتر باشد.")]
    public string? Tags { get; set; }

    public IReadOnlyCollection<string> TagItems { get; set; }
        = Array.Empty<string>();

    [Display(Name = "تصویر شاخص")]
    [MaxLength(600, ErrorMessage = "آدرس تصویر شاخص نباید بیش از ۶۰۰ کاراکتر باشد.")]
    public string? FeaturedImagePath { get; set; }

    [Display(Name = "آپلود تصویر شاخص")]
    public IFormFile? FeaturedImageUpload { get; set; }

    [Display(Name = "لینک فایل دانلودی")]
    public string? DigitalDownloadPath { get; set; }

    public bool WasPreviouslyPublished { get; set; }

    public IReadOnlyCollection<SelectListItem> CategoryOptions { get; set; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> TypeOptions { get; set; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<string> TagSuggestions { get; set; }
        = Array.Empty<string>();
}

public sealed class TeacherProductFilterViewModel
{
    public string? SearchTerm { get; init; }

    public ProductType? Type { get; init; }

    public TeacherProductStatusFilter? Status { get; init; }

    public bool HasValues
        => !string.IsNullOrWhiteSpace(SearchTerm)
           || Type.HasValue
           || Status.HasValue;
}

public sealed class TeacherProductFilterRequest
{
    public string? SearchTerm { get; init; }

    public ProductType? Type { get; init; }

    public TeacherProductStatusFilter? Status { get; init; }
}

public enum TeacherProductStatusFilter
{
    Published = 1,
    Pending = 2
}

public sealed record TeacherProductGalleryItemViewModel(Guid Id, string Path, int Order);

public sealed record TeacherProductSalesTrendPointViewModel(string Label, decimal Quantity, decimal Revenue);

public sealed record TeacherProductSalesSummaryViewModel(
    int TotalOrders,
    decimal TotalQuantity,
    decimal TotalRevenue,
    decimal TotalDiscount,
    decimal AverageOrderValue,
    DateTimeOffset? FirstSaleAt,
    DateTimeOffset? LastSaleAt,
    IReadOnlyCollection<TeacherProductSalesTrendPointViewModel> Trend)
{
    public static readonly TeacherProductSalesSummaryViewModel Empty = new(
        0,
        0m,
        0m,
        0m,
        0m,
        null,
        null,
        Array.Empty<TeacherProductSalesTrendPointViewModel>());
}

public sealed class TeacherProductDetailViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public ProductType Type { get; init; }

    public decimal Price { get; init; }

    public decimal? CompareAtPrice { get; init; }

    public bool TrackInventory { get; init; }

    public int StockQuantity { get; init; }

    public bool IsPublished { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public string? FeaturedImagePath { get; init; }

    public string? DigitalDownloadPath { get; init; }

    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<TeacherProductGalleryItemViewModel> Gallery { get; init; }
        = Array.Empty<TeacherProductGalleryItemViewModel>();

    public TeacherProductSalesSummaryViewModel Sales { get; init; }
        = TeacherProductSalesSummaryViewModel.Empty;
}
