using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LogsDtoCloneTest.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogsDtoCloneTest.WebSite.Areas.Seller.Models;

public sealed record SellerProductListItemViewModel(
    Guid Id,
    string Name,
    string Category,
    ProductType Type,
    decimal? Price,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdatedAt,
    string? FeaturedImagePath,
    IReadOnlyCollection<string> Tags);

public sealed class SellerProductIndexViewModel
{
    public IReadOnlyCollection<SellerProductListItemViewModel> Products { get; init; }
        = Array.Empty<SellerProductListItemViewModel>();

    public int TotalCount { get; init; }

    public int PendingCount { get; init; }

    public int PublishedCount { get; init; }

    public string? SuccessMessage { get; init; }

    public string? ErrorMessage { get; init; }

    public SellerProductFilterViewModel Filter { get; init; } = new();

    public bool HasActiveFilters => Filter.HasValues;
}

public enum ProductRequestType
{
    NewProduct = 1,
    OfferForExistingProduct = 2
}

public sealed class SellerProductFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "نوع درخواست")]
    public ProductRequestType RequestType { get; set; } = ProductRequestType.NewProduct;

    [Display(Name = "محصول موجود (برای پیشنهاد)")]
    public Guid? ExistingProductId { get; set; }

    [Display(Name = "نام محصول موجود")]
    public string? ExistingProductName { get; set; }

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
    public decimal? Price { get; set; }

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

    [Display(Name = "برند")]
    [MaxLength(200, ErrorMessage = "نام برند نباید بیش از ۲۰۰ کاراکتر باشد.")]
    public string? Brand { get; set; }

    public bool WasPreviouslyPublished { get; set; }

    public IReadOnlyCollection<SelectListItem> CategoryOptions { get; set; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> TypeOptions { get; set; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<string> TagSuggestions { get; set; }
        = Array.Empty<string>();
}

public sealed class SellerProductFilterViewModel
{
    public string? SearchTerm { get; init; }

    public ProductType? Type { get; init; }

    public SellerProductStatusFilter? Status { get; init; }

    public bool HasValues
        => !string.IsNullOrWhiteSpace(SearchTerm)
           || Type.HasValue
           || Status.HasValue;
}

public sealed class SellerProductFilterRequest
{
    public string? SearchTerm { get; init; }

    public ProductType? Type { get; init; }

    public SellerProductStatusFilter? Status { get; init; }
}

public enum SellerProductStatusFilter
{
    Published = 1,
    Pending = 2
}

public sealed record SellerProductGalleryItemViewModel(Guid Id, string Path, int Order);

public sealed record SellerProductSalesTrendPointViewModel(string Label, decimal Quantity, decimal Revenue);

public sealed record SellerProductSalesSummaryViewModel(
    int TotalOrders,
    decimal TotalQuantity,
    decimal TotalRevenue,
    decimal TotalDiscount,
    decimal AverageOrderValue,
    DateTimeOffset? FirstSaleAt,
    DateTimeOffset? LastSaleAt,
    IReadOnlyCollection<SellerProductSalesTrendPointViewModel> Trend)
{
    public static readonly SellerProductSalesSummaryViewModel Empty = new(
        0,
        0m,
        0m,
        0m,
        0m,
        null,
        null,
        Array.Empty<SellerProductSalesTrendPointViewModel>());
}

public sealed class SellerProductDetailViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public ProductType Type { get; init; }

    public decimal? Price { get; init; }

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

    public IReadOnlyCollection<SellerProductGalleryItemViewModel> Gallery { get; init; }
        = Array.Empty<SellerProductGalleryItemViewModel>();

    public SellerProductSalesSummaryViewModel Sales { get; init; }
        = SellerProductSalesSummaryViewModel.Empty;

    public int ViewCount { get; init; }
}

public sealed record SellerProductAttributeListItemViewModel(
    Guid Id,
    string Key,
    string Value,
    int DisplayOrder);

public sealed class SellerProductAttributeFormModel
{
    public Guid? Id { get; set; }

    [Display(Name = "کلید")]
    [Required(ErrorMessage = "کلید ویژگی را وارد کنید.")]
    [MaxLength(200, ErrorMessage = "کلید ویژگی نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string Key { get; set; } = string.Empty;

    [Display(Name = "مقدار")]
    [Required(ErrorMessage = "مقدار ویژگی را وارد کنید.")]
    [MaxLength(500, ErrorMessage = "مقدار ویژگی نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")]
    public string Value { get; set; } = string.Empty;

    [Display(Name = "ترتیب نمایش")]
    [Range(0, int.MaxValue, ErrorMessage = "ترتیب نمایش نمی‌تواند منفی باشد.")]
    public int DisplayOrder { get; set; }
}

public sealed class SellerProductAttributesViewModel
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public IReadOnlyCollection<SellerProductAttributeListItemViewModel> Attributes { get; init; }
        = Array.Empty<SellerProductAttributeListItemViewModel>();

    public SellerProductAttributeFormModel Form { get; init; } = new();

    public Guid? HighlightedAttributeId { get; init; }
}

public sealed record SellerCommentItemViewModel(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string AuthorName,
    string Content,
    int? Rating,
    DateTimeOffset CreatedAt,
    bool IsApproved);

public sealed class SellerAllCommentsViewModel
{
    public IReadOnlyCollection<SellerCommentItemViewModel> NewComments { get; set; }
        = Array.Empty<SellerCommentItemViewModel>();

    public IReadOnlyCollection<SellerCommentItemViewModel> ApprovedComments { get; set; }
        = Array.Empty<SellerCommentItemViewModel>();
}

public sealed record SellerProductVariantAttributeListItemViewModel(
    Guid Id,
    string Name,
    IReadOnlyCollection<string> Options,
    int DisplayOrder);

public sealed class SellerProductVariantAttributeFormModel
{
    public Guid? Id { get; set; }

    [Display(Name = "نام ویژگی")]
    [Required(ErrorMessage = "نام ویژگی الزامی است.")]
    [MaxLength(100, ErrorMessage = "نام ویژگی نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "گزینه‌ها (جدا شده با کاما)")]
    public string OptionsText { get; set; } = string.Empty;

    public List<string> Options { get; set; } = new();

    [Display(Name = "ترتیب نمایش")]
    [Range(0, 100, ErrorMessage = "ترتیب نمایش باید بین ۰ تا ۱۰۰ باشد.")]
    public int DisplayOrder { get; set; }
}

public sealed class SellerProductVariantAttributesViewModel
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public IReadOnlyCollection<SellerProductVariantAttributeListItemViewModel> VariantAttributes { get; init; }
        = Array.Empty<SellerProductVariantAttributeListItemViewModel>();

    public SellerProductVariantAttributeFormModel Form { get; init; } = new();

    public Guid? HighlightedVariantAttributeId { get; init; }
}

public sealed record SellerProductVariantListItemViewModel(
    Guid Id,
    decimal? Price,
    decimal? CompareAtPrice,
    int StockQuantity,
    string? Sku,
    string? ImagePath,
    bool IsActive,
    IReadOnlyCollection<(Guid VariantAttributeId, string Value)> Options);

public sealed class SellerProductVariantFormModel
{
    public Guid? Id { get; set; }

    [Display(Name = "قیمت")]
    [Range(0, double.MaxValue, ErrorMessage = "قیمت نامعتبر است.")]
    public decimal? Price { get; set; }

    [Display(Name = "قیمت قبل از تخفیف")]
    [Range(0, double.MaxValue, ErrorMessage = "قیمت قبل از تخفیف نامعتبر است.")]
    public decimal? CompareAtPrice { get; set; }

    [Display(Name = "موجودی")]
    [Range(0, int.MaxValue, ErrorMessage = "موجودی نمی‌تواند منفی باشد.")]
    public int StockQuantity { get; set; }

    [Display(Name = "SKU (کد محصول)")]
    [MaxLength(100, ErrorMessage = "SKU (کد محصول) نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.")]
    public string? Sku { get; set; }

    [Display(Name = "تصویر گزینه")]
    public IFormFile? Image { get; set; }

    public string? ImagePath { get; set; }

    [Display(Name = "حذف تصویر")]
    public bool RemoveImage { get; set; }

    [Display(Name = "فعال")]
    public bool IsActive { get; set; } = true;

    // گزینه‌ها: Dictionary of VariantAttributeId -> Value
    public Dictionary<Guid, string> Options { get; set; } = new();
}

public sealed class SellerProductVariantsViewModel
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public IReadOnlyCollection<SellerProductVariantAttributeListItemViewModel> VariantAttributes { get; init; }
        = Array.Empty<SellerProductVariantAttributeListItemViewModel>();

    public IReadOnlyCollection<SellerProductVariantListItemViewModel> Variants { get; init; }
        = Array.Empty<SellerProductVariantListItemViewModel>();

    public SellerProductVariantFormModel Form { get; init; } = new();

    public Guid? HighlightedVariantId { get; init; }
}
