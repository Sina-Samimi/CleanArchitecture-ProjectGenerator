using System.Collections.Generic;

namespace TestAttarClone.WebSite.Models.Product;

public class ProductDetailViewModel
{
    public required ProductSummaryViewModel Product { get; init; }

    public required string HeroImageUrl { get; init; }

    public required string Description { get; init; }

    public string? DifficultyLevel { get; init; }

    public string? Duration { get; init; }

    public IReadOnlyList<string> Highlights { get; init; } = System.Array.Empty<string>();

    public IReadOnlyList<ProductModuleViewModel> Modules { get; init; } = System.Array.Empty<ProductModuleViewModel>();

    public IReadOnlyList<ProductStatisticViewModel> Statistics { get; init; } = System.Array.Empty<ProductStatisticViewModel>();

    public IReadOnlyList<ProductFaqItemViewModel> FaqItems { get; init; } = System.Array.Empty<ProductFaqItemViewModel>();

    public IReadOnlyList<ProductAttributeViewModel> Attributes { get; init; } = System.Array.Empty<ProductAttributeViewModel>();

    public IReadOnlyList<ProductVariantAttributeViewModel> VariantAttributes { get; init; } = System.Array.Empty<ProductVariantAttributeViewModel>();

    public IReadOnlyList<ProductVariantViewModel> Variants { get; init; } = System.Array.Empty<ProductVariantViewModel>();

    public IReadOnlyList<ProductCommentViewModel> Comments { get; init; } = System.Array.Empty<ProductCommentViewModel>();

    public IReadOnlyList<ProductSummaryViewModel> RelatedProducts { get; init; } = System.Array.Empty<ProductSummaryViewModel>();

    public ProductCommentFormModel NewComment { get; init; } = new();

    public IReadOnlyList<ProductOfferViewModel> Offers { get; init; } = System.Array.Empty<ProductOfferViewModel>();

    public IReadOnlyList<SuggestedProductOfferViewModel> SuggestedProductOffers { get; init; } = System.Array.Empty<SuggestedProductOfferViewModel>();

    public IReadOnlyList<ProductGalleryItemViewModel> Gallery { get; init; } = System.Array.Empty<ProductGalleryItemViewModel>();

    public bool IsSiteProduct { get; init; }

    public bool CanAddToCart { get; init; } = true;

    public int StockQuantity { get; init; }

    public bool TrackInventory { get; init; }

    /// <summary>
    /// آیا کاربر فعلی (در صورت لاگین بودن) قبلاً برای این محصول درخواست «خبرم کن» ثبت کرده است؟
    /// فقط برای محصول پایه (نه پیشنهاد فروشنده) استفاده می‌شود.
    /// </summary>
    public bool HasBackInStockSubscription { get; init; }
}

public class ProductGalleryItemViewModel
{
    public required Guid Id { get; init; }
    
    public required string ImagePath { get; init; }
    
    public int DisplayOrder { get; init; }
}

public class ProductOfferViewModel
{
    public required Guid Id { get; init; }
    
    public required string SellerId { get; init; }
    
    public required string SellerName { get; init; }
    
    public decimal? Price { get; init; }
    
    public decimal? CompareAtPrice { get; init; }
    
    public bool TrackInventory { get; init; }
    
    public int StockQuantity { get; init; }
    
    public bool IsInStock => !TrackInventory || StockQuantity > 0;
    
    public string GetFormattedPrice()
    {
        if (!Price.HasValue)
            return "قیمت توافقی";
        
        var culture = new System.Globalization.CultureInfo("fa-IR");
        return $"{Price.Value:N0} تومان";
    }
    
    public string? GetFormattedCompareAtPrice()
    {
        if (!CompareAtPrice.HasValue)
            return null;
        
        var culture = new System.Globalization.CultureInfo("fa-IR");
        return $"{CompareAtPrice.Value:N0} تومان";
    }
    
    public decimal? GetDiscountPercentage()
    {
        if (!CompareAtPrice.HasValue || !Price.HasValue || CompareAtPrice.Value <= 0)
            return null;
        
        if (Price.Value >= CompareAtPrice.Value)
            return null;
        
        return Math.Round((1 - (Price.Value / CompareAtPrice.Value)) * 100, 0);
    }
}

public class SuggestedProductOfferViewModel
{
    public required Guid Id { get; init; }
    
    public required string SellerId { get; init; }
    
    public required string SellerName { get; init; }
    
    public required string ProductName { get; init; }
    
    public required string CategoryName { get; init; }
    
    public required string ImageUrl { get; init; }
    
    public string? ShopAddress { get; init; }
    
    public decimal? Price { get; init; }
    
    public bool TrackInventory { get; init; }
    
    public int StockQuantity { get; init; }
    
    public bool IsInStock => !TrackInventory || StockQuantity > 0;
    
    public DateTimeOffset CreatedAt { get; init; }
    
    public string GetFormattedPrice()
    {
        if (!Price.HasValue)
            return "قیمت توافقی";
        
        var culture = new System.Globalization.CultureInfo("fa-IR");
        return $"{Price.Value:N0}";
    }
}

public class ProductModuleViewModel
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? Duration { get; init; }
}

public class ProductStatisticViewModel
{
    public required string Label { get; init; }

    public required string Value { get; init; }

    public string? Tooltip { get; init; }
}

public class ProductFaqItemViewModel
{
    public required string Question { get; init; }

    public required string Answer { get; init; }
}

public class ProductAttributeViewModel
{
    public required string Key { get; init; }
    
    public required string Value { get; init; }
}

public class ProductVariantAttributeViewModel
{
    public required Guid Id { get; init; }
    
    public required string Name { get; init; }
    
    public required IReadOnlyList<string> Options { get; init; }
    
    public int DisplayOrder { get; init; }
}

public class ProductVariantOptionViewModel
{
    public required Guid Id { get; init; }
    
    public required Guid VariantAttributeId { get; init; }
    
    public required string Value { get; init; }
}

public class ProductVariantViewModel
{
    public required Guid Id { get; init; }
    
    public decimal? Price { get; init; }
    
    public decimal? CompareAtPrice { get; init; }
    
    public int StockQuantity { get; init; }
    
    public string? Sku { get; init; }
    
    public string? ImagePath { get; init; }
    
    public bool IsActive { get; init; }
    
    public required IReadOnlyList<ProductVariantOptionViewModel> Options { get; init; }
}
