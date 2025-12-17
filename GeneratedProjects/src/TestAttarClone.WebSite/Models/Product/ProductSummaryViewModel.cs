using System;
using System.Collections.Generic;
using System.Globalization;

namespace TestAttarClone.WebSite.Models.Product;

public class ProductSummaryViewModel
{
    public Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public required string ShortDescription { get; init; }

    public decimal? Price { get; init; }

    public decimal? OriginalPrice { get; init; }

    public bool IsCustomOrder { get; init; }

    public required string ThumbnailUrl { get; init; }

    public string? Category { get; init; }

    public string? DeliveryFormat { get; init; }

    public double Rating { get; init; }

    public int ReviewCount { get; init; }

    public string? Badge { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public string GetFormattedPrice() => Price.HasValue 
        ? Price.Value.ToString("N0", CultureInfo.GetCultureInfo("fa-IR"))
        : "قیمت بر اساس سفارش";

    public string? GetFormattedOriginalPrice() => OriginalPrice.HasValue
        ? OriginalPrice.Value.ToString("N0", CultureInfo.GetCultureInfo("fa-IR"))
        : null;

    public string GetRatingLabel() => Rating.ToString("0.0", CultureInfo.InvariantCulture);

    public string GetReviewCountLabel() => string.Format(CultureInfo.GetCultureInfo("fa-IR"), "{0:N0} نظر", ReviewCount);
}
