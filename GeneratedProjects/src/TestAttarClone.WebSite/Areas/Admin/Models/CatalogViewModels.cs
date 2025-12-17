using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TestAttarClone.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TestAttarClone.WebSite.Areas.Admin.Models;

public sealed record ProductListItemViewModel(
    Guid Id,
    string Name,
    string Category,
    Guid CategoryId,
    ProductType Type,
    decimal? Price,
    decimal? CompareAtPrice,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    string? FeaturedImagePath,
    string TagList,
    DateTimeOffset UpdatedAt,
    string? SellerId,
    string? SellerName,
    string? SellerPhone,
    int SellerCount,
    int ViolationCount = 0);

public sealed record ProductListStatisticsViewModel(
    int TotalProducts,
    int FilteredProducts,
    int PublishedProducts,
    int DraftProducts,
    int PhysicalProducts,
    int DigitalProducts,
    decimal AveragePrice,
    decimal HighestPrice,
    decimal LowestPrice);

public sealed class ProductIndexFilterViewModel
{
    public string? Search { get; set; }

    public Guid? CategoryId { get; set; }

    public ProductType? Type { get; set; }

    public bool? IsPublished { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "حداقل قیمت معتبر نیست.")]
    public decimal? MinPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "حداکثر قیمت معتبر نیست.")]
    public decimal? MaxPrice { get; set; }

    public string? SellerId { get; set; }

    [Range(1, 100, ErrorMessage = "تعداد نمایش در صفحه خارج از محدوده است.")]
    public int PageSize { get; set; } = 12;
}

public sealed class ProductIndexRequest
{
    public string? Search { get; set; }

    public Guid? CategoryId { get; set; }

    public ProductType? Type { get; set; }

    public bool? IsPublished { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public string? SellerId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 12;
}

public sealed class ProductIndexViewModel
{
    public IReadOnlyCollection<ProductListItemViewModel> Products { get; init; }
        = Array.Empty<ProductListItemViewModel>();

    public ProductListStatisticsViewModel Statistics { get; init; }
        = new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    public ProductIndexFilterViewModel Filters { get; init; } = new();

    public IReadOnlyCollection<SelectListItem> CategoryOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> TypeOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> StatusOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<int> PageSizeOptions { get; init; }
        = Array.Empty<int>();

    public IReadOnlyCollection<string> TagSuggestions { get; init; }
        = Array.Empty<string>();

    public int TotalCount { get; init; }

    public int FilteredCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalPages { get; init; }

    public int FirstItemIndex { get; init; }

    public int LastItemIndex { get; init; }
}

public sealed class ProductCategoriesViewModel
{
    public IReadOnlyCollection<ProductCategoryTreeItemViewModel> Tree { get; init; }
        = Array.Empty<ProductCategoryTreeItemViewModel>();

    public IReadOnlyCollection<ProductCategoryFlatItemViewModel> Categories { get; init; }
        = Array.Empty<ProductCategoryFlatItemViewModel>();

    public ProductCategoryStatisticsViewModel Statistics { get; init; }
        = new(0, 0, 0, 0, 0);

    public ProductCategoryFormModel CreateCategory { get; init; } = new();

    public ProductCategoryUpdateFormModel EditCategory { get; init; } = new();

    public IReadOnlyCollection<SelectListItem> CreateParentOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> EditParentOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public Guid? HighlightedCategoryId { get; init; }
}

public sealed record ProductCategoryTreeItemViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Slug { get; init; }

    public string? Description { get; init; }

    public Guid? ParentId { get; init; }

    public int Depth { get; init; }

    public IReadOnlyCollection<ProductCategoryTreeItemViewModel> Children { get; init; }
        = Array.Empty<ProductCategoryTreeItemViewModel>();

    public IReadOnlyCollection<Guid> DescendantIds { get; init; }
        = Array.Empty<Guid>();
}

public sealed record ProductCategoryTreePartialModel(
    IReadOnlyCollection<ProductCategoryTreeItemViewModel> Nodes,
    Guid? HighlightedCategoryId);

public sealed record ProductCategoryFlatItemViewModel(
    Guid Id,
    string Name,
    string? Slug,
    string? Description,
    Guid? ParentId,
    string? ParentName,
    int Depth,
    int ChildCount,
    int DescendantCount);

public sealed record ProductCategoryStatisticsViewModel(
    int Total,
    int RootCount,
    int BranchCount,
    int LeafCount,
    int MaxDepth);

public class ProductCategoryFormModel
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

    [Display(Name = "تصویر")]
    public IFormFile? ImageFile { get; set; }

    public string? ImageUrl { get; set; }
}

public sealed class ProductCategoryUpdateFormModel : ProductCategoryFormModel
{
    [Required(ErrorMessage = "شناسه دسته‌بندی معتبر نیست.")]
    public Guid Id { get; set; }
}

public sealed record ProductExecutionStepListItemViewModel(
    Guid Id,
    string Title,
    string? Description,
    string? Duration,
    int Order);

public sealed class ProductExecutionStepFormModel
{
    public Guid? Id { get; set; }

    [Display(Name = "عنوان گام")]
    [Required(ErrorMessage = "عنوان گام را وارد کنید.")]
    [MaxLength(200, ErrorMessage = "عنوان گام نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "توضیحات گام")]
    [MaxLength(1000, ErrorMessage = "توضیحات گام نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.")]
    public string? Description { get; set; }

    [Display(Name = "مدت زمان")]
    [MaxLength(100, ErrorMessage = "مدت زمان نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.")]
    public string? Duration { get; set; }

    [Display(Name = "ترتیب نمایش")]
    [Range(0, 100, ErrorMessage = "ترتیب نمایش باید بین ۰ تا ۱۰۰ باشد.")]
    public int Order { get; set; }
}

public sealed class ProductExecutionStepsViewModel
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public IReadOnlyCollection<ProductExecutionStepListItemViewModel> Steps { get; init; }
        = Array.Empty<ProductExecutionStepListItemViewModel>();

    public ProductExecutionStepFormModel Form { get; init; } = new();

    public Guid? HighlightedStepId { get; init; }
        = null;
}

public sealed record ProductGalleryItemViewModel(Guid Id, string Path, int Order);

public sealed record ProductSalesTrendPointViewModel(string Label, decimal Quantity, decimal Revenue);

public sealed record ProductSalesSummaryViewModel(
    int TotalOrders,
    decimal TotalQuantity,
    decimal TotalRevenue,
    decimal TotalDiscount,
    decimal AverageOrderValue,
    DateTimeOffset? FirstSaleAt,
    DateTimeOffset? LastSaleAt,
    IReadOnlyCollection<ProductSalesTrendPointViewModel> Trend)
{
    public static readonly ProductSalesSummaryViewModel Empty = new(
        0,
        0m,
        0m,
        0m,
        0m,
        null,
        null,
        Array.Empty<ProductSalesTrendPointViewModel>());
}

public sealed class ProductDetailViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public ProductType Type { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public decimal? Price { get; init; }

    public decimal? CompareAtPrice { get; init; }

    public bool IsCustomOrder { get; init; }

    public bool TrackInventory { get; init; }

    public int StockQuantity { get; init; }

    public bool IsPublished { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public string SeoTitle { get; init; } = string.Empty;

    public string SeoDescription { get; init; } = string.Empty;

    public string SeoKeywords { get; init; } = string.Empty;

    public string SeoSlug { get; init; } = string.Empty;

    public string Robots { get; init; } = string.Empty;

    public string TagList { get; init; } = string.Empty;

    public string? FeaturedImagePath { get; init; }

    public string? DigitalDownloadPath { get; init; }

    public IReadOnlyCollection<ProductGalleryItemViewModel> Gallery { get; init; }
        = Array.Empty<ProductGalleryItemViewModel>();

    public ProductSalesSummaryViewModel Sales { get; init; } = ProductSalesSummaryViewModel.Empty;

    public int ViewCount { get; init; }
}

public sealed record ProductExecutionStepSummaryViewModel(
    Guid ProductId,
    string ProductName,
    string CategoryName,
    ProductType Type,
    bool IsPublished,
    int StepCount,
    DateTimeOffset? UpdatedAt);

public sealed class ProductExecutionStepsOverviewViewModel
{
    public int TotalProducts { get; init; }

    public int ProductsWithSteps { get; init; }

    public int TotalSteps { get; init; }

    public double AverageStepsPerProduct { get; init; }

    public IReadOnlyCollection<ProductExecutionStepSummaryViewModel> Items { get; init; }
        = Array.Empty<ProductExecutionStepSummaryViewModel>();

    public int ProductsWithoutSteps => Math.Max(0, TotalProducts - ProductsWithSteps);
}

public sealed record ProductAttributeListItemViewModel(
    Guid Id,
    string Key,
    string Value,
    int DisplayOrder);

public sealed class ProductAttributeFormModel
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

public sealed class ProductAttributesViewModel
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string ProductSlug { get; init; } = string.Empty;

    public IReadOnlyCollection<ProductAttributeListItemViewModel> Attributes { get; init; }
        = Array.Empty<ProductAttributeListItemViewModel>();

    public ProductAttributeFormModel Form { get; init; } = new();

    public Guid? HighlightedAttributeId { get; init; }
}

public sealed record ProductFaqListItemViewModel(
    Guid Id,
    string Question,
    string Answer,
    int Order);

public sealed class ProductFaqFormModel
{
    public Guid? Id { get; set; }

    [Display(Name = "سوال")]
    [Required(ErrorMessage = "سوال را وارد کنید.")]
    [MaxLength(300, ErrorMessage = "سوال نمی‌تواند بیش از ۳۰۰ کاراکتر باشد.")]
    public string Question { get; set; } = string.Empty;

    [Display(Name = "پاسخ")]
    [Required(ErrorMessage = "پاسخ سوال را وارد کنید.")]
    [MaxLength(2000, ErrorMessage = "پاسخ نمی‌تواند بیش از ۲۰۰۰ کاراکتر باشد.")]
    public string Answer { get; set; } = string.Empty;

    [Display(Name = "ترتیب نمایش")]
    [Range(0, 100, ErrorMessage = "ترتیب نمایش باید بین ۰ تا ۱۰۰ باشد.")]
    public int Order { get; set; }
}

public sealed class ProductFaqsViewModel
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public IReadOnlyCollection<ProductFaqListItemViewModel> Faqs { get; init; }
        = Array.Empty<ProductFaqListItemViewModel>();

    public ProductFaqFormModel Form { get; init; } = new();

    public Guid? HighlightedFaqId { get; init; }
        = null;
}

public sealed class ProductGalleryItemFormModel
{
    public Guid? Id { get; set; }

    [Display(Name = "آدرس تصویر")]
    [MaxLength(500, ErrorMessage = "آدرس تصویر نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")]
    public string? Path { get; set; }

    [Display(Name = "فایل تصویر")]
    public IFormFile? Image { get; set; }

    [Display(Name = "ترتیب نمایش")]
    [Range(0, 1000, ErrorMessage = "ترتیب نمایش باید بین ۰ تا ۱۰۰۰ باشد.")]
    public int Order { get; set; }

    [Display(Name = "حذف تصویر")]
    public bool Remove { get; set; }
}

public enum ProductPublishStatus
{
    Draft = 0,
    Published = 1,
    Scheduled = 2
}

public sealed class ProductFormSelectionsViewModel
{
    public IReadOnlyCollection<SelectListItem> CategoryOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> TypeOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> PublishStatusOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> RobotsOptions { get; init; }
        = Array.Empty<SelectListItem>();

    public IReadOnlyCollection<SelectListItem> SellerOptions { get; init; }
        = Array.Empty<SelectListItem>();
}

public sealed class ProductFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "نام محصول")]
    [Required(ErrorMessage = "نام محصول را وارد کنید.")]
    [MaxLength(200, ErrorMessage = "نام محصول نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "خلاصه کوتاه")]
    [MaxLength(500, ErrorMessage = "خلاصه نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")]
    public string? Summary { get; set; }

    [Display(Name = "توضیحات کامل")]
    [Required(ErrorMessage = "توضیحات محصول را وارد کنید.")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "نوع محصول")]
    [Required(ErrorMessage = "نوع محصول را انتخاب کنید.")]
    public ProductType Type { get; set; } = ProductType.Physical;

    [Display(Name = "محصول سفارشی")]
    public bool IsCustomOrder { get; set; }

    [Display(Name = "قیمت")]
    [Range(0, double.MaxValue, ErrorMessage = "قیمت محصول نامعتبر است.")]
    public decimal? Price { get; set; }

    [Display(Name = "قیمت قبل از تخفیف")]
    [Range(0, double.MaxValue, ErrorMessage = "قیمت قبل از تخفیف نامعتبر است.")]
    public decimal? CompareAtPrice { get; set; }

    [Display(Name = "مدیریت موجودی")]
    public bool TrackInventory { get; set; } = true;

    [Display(Name = "موجودی انبار")]
    [Range(0, int.MaxValue, ErrorMessage = "موجودی نمی‌تواند منفی باشد.")]
    public int StockQuantity { get; set; }

    [Display(Name = "دسته‌بندی")]
    [Required(ErrorMessage = "دسته‌بندی محصول را انتخاب کنید.")]
    public Guid? CategoryId { get; set; }

    [Display(Name = "شناسه فروشنده")]
    [MaxLength(450, ErrorMessage = "شناسه فروشنده نمی‌تواند بیش از ۴۵۰ کاراکتر باشد.")]
    public string? SellerId { get; set; }

    [Display(Name = "برند")]
    [MaxLength(200, ErrorMessage = "نام برند نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string? Brand { get; set; }

    [Display(Name = "وضعیت انتشار")]
    public ProductPublishStatus PublishStatus { get; set; } = ProductPublishStatus.Draft;

    [Display(Name = "تاریخ انتشار")]
    public DateTimeOffset? PublishedAt { get; set; }

    [Display(Name = "تاریخ انتشار (شمسی)")]
    public string? PublishedAtPersian { get; set; }

    [Display(Name = "ساعت انتشار")]
    public string? PublishedAtTime { get; set; }

    [Display(Name = "عنوان سئو")]
    [Required(ErrorMessage = "عنوان سئو را وارد کنید.")]
    [MaxLength(200, ErrorMessage = "عنوان سئو نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string SeoTitle { get; set; } = string.Empty;

    [Display(Name = "توضیحات سئو")]
    [MaxLength(500, ErrorMessage = "توضیحات سئو نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")]
    public string? SeoDescription { get; set; }

    [Display(Name = "کلمات کلیدی سئو")]
    [MaxLength(500, ErrorMessage = "کلمات کلیدی نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")]
    public string? SeoKeywords { get; set; }

    [Display(Name = "مسیر سئو (Slug)")]
    [Required(ErrorMessage = "مسیر سئو را وارد کنید.")]
    [MaxLength(200, ErrorMessage = "مسیر سئو نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string SeoSlug { get; set; } = string.Empty;

    [Display(Name = "دستورالعمل ربات‌ها")]
    [MaxLength(100, ErrorMessage = "Robots نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.")]
    public string Robots { get; set; } = "index,follow";

    [Display(Name = "برچسب‌ها")]
    public string Tags { get; set; } = string.Empty;

    public IReadOnlyCollection<string> TagItems { get; set; }
        = Array.Empty<string>();

    [Display(Name = "تصویر شاخص")]
    public IFormFile? FeaturedImage { get; set; }

    public string? FeaturedImagePath { get; set; }

    [Display(Name = "حذف تصویر فعلی")]
    public bool RemoveFeaturedImage { get; set; }

    [Display(Name = "لینک دانلود")]
    [MaxLength(500, ErrorMessage = "مسیر فایل نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.")]
    public string? DigitalDownloadPath { get; set; }

    [Display(Name = "فایل دانلودی (آپلود)")]
    public IFormFile? DigitalDownloadFile { get; set; }

    public List<ProductGalleryItemFormModel> Gallery { get; set; } = new();

    public ProductFormSelectionsViewModel Selections { get; set; } = new();

    // ویژگی‌های گزینه
    public List<ProductVariantAttributeFormModel> VariantAttributes { get; set; } = new();

    // گزینه‌ها
    public List<ProductVariantFormModel> Variants { get; set; } = new();
}

public sealed class ProductVariantAttributeFormModel
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

public sealed class ProductVariantFormModel
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

public sealed class ProductCommentItemViewModel
{
    public Guid Id { get; init; }

    public Guid? ParentId { get; init; }

    public string AuthorName { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public double Rating { get; init; }

    public bool IsApproved { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string? ApprovedByName { get; init; }

    public DateTimeOffset? ApprovedAt { get; init; }

    public string? ParentAuthorName { get; init; }

    public string? ParentExcerpt { get; init; }
}

public sealed class ProductCommentListViewModel
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string? ProductSlug { get; init; }

    public int TotalCount { get; init; }

    public int ApprovedCount { get; init; }

    public int PendingCount { get; init; }

    public double AverageRating { get; init; }

    public IReadOnlyCollection<ProductCommentItemViewModel> Comments { get; init; } = Array.Empty<ProductCommentItemViewModel>();

    public string DefaultAuthorName { get; init; } = "مدیر سیستم";
}

public sealed class AdminProductCommentReplyViewModel
{
    [System.ComponentModel.DataAnnotations.Display(Name = "نام پاسخ‌دهنده")]
    [System.ComponentModel.DataAnnotations.MaxLength(200, ErrorMessage = "نام نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.")]
    public string? AuthorName { get; set; }

    [System.ComponentModel.DataAnnotations.Display(Name = "متن پاسخ")]
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "متن پاسخ را وارد کنید.")]
    [System.ComponentModel.DataAnnotations.MaxLength(2000, ErrorMessage = "متن پاسخ نمی‌تواند بیش از ۲۰۰۰ کاراکتر باشد.")]
    public string Content { get; set; } = string.Empty;
}

public sealed class PendingProductCommentItemViewModel
{
    public Guid Id { get; init; }

    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string? ProductSlug { get; init; }

    public string AuthorName { get; init; } = string.Empty;

    public string Excerpt { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class PendingProductCommentListViewModel
{
    public int TotalCount { get; init; }

    public IReadOnlyCollection<PendingProductCommentItemViewModel> Items { get; init; } = Array.Empty<PendingProductCommentItemViewModel>();
}
