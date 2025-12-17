using System;
using System.Collections.Generic;

namespace LogsDtoCloneTest.WebSite.Models.Product;

public class Product
{
    public Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public required string ShortDescription { get; init; }

    public required string Description { get; init; }

    public required string HeroImageUrl { get; init; }

    public required string ThumbnailUrl { get; init; }

    public decimal? Price { get; init; }

    public decimal? OriginalPrice { get; init; }

    public bool IsCustomOrder { get; init; }

    public double Rating { get; init; }

    public int ReviewCount { get; init; }

    public string? DifficultyLevel { get; init; }

    public string? Category { get; init; }

    public string? DeliveryFormat { get; init; }

    public string? Duration { get; init; }

    public DateTimeOffset PublishedAt { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Highlights { get; init; } = Array.Empty<string>();

    public IReadOnlyList<ProductModule> Modules { get; init; } = Array.Empty<ProductModule>();

    public IReadOnlyList<ProductStatistic> Statistics { get; init; } = Array.Empty<ProductStatistic>();

    public IReadOnlyList<ProductFaqItem> FaqItems { get; init; } = Array.Empty<ProductFaqItem>();

    public IReadOnlyList<ProductAttribute> Attributes { get; init; } = Array.Empty<ProductAttribute>();

    public IReadOnlyList<ProductComment> Comments { get; init; } = Array.Empty<ProductComment>();

    public IReadOnlyList<ProductGalleryImage> Gallery { get; init; } = Array.Empty<ProductGalleryImage>();

    public bool IsFeatured { get; init; }
}

public class ProductGalleryImage
{
    public required Guid Id { get; init; }
    
    public required string ImagePath { get; init; }
    
    public int DisplayOrder { get; init; }
}

public class ProductModule
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? Duration { get; init; }
}

public class ProductStatistic
{
    public required string Label { get; init; }

    public required string Value { get; init; }

    public string? Tooltip { get; init; }
}

public class ProductFaqItem
{
    public required string Question { get; init; }

    public required string Answer { get; init; }
}

public class ProductAttribute
{
    public required string Key { get; init; }

    public required string Value { get; init; }
}

public class ProductComment
{
    public Guid Id { get; init; }

    public Guid? ParentId { get; init; }

    public required string AuthorName { get; init; }

    public required string Content { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public double Rating { get; init; }
}
