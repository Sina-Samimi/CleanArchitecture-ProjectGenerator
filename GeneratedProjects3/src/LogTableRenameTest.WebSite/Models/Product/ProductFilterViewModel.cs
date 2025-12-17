using System.Collections.Generic;

namespace LogTableRenameTest.WebSite.Models.Product;

public class ProductFilterViewModel
{
    public string? SearchTerm { get; init; }

    public string? SelectedCategory { get; init; }

    public string? SelectedDeliveryFormat { get; init; }

    public string? SelectedSort { get; init; }

    public double? MinRating { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public decimal PriceRangeMin { get; init; }

    public decimal PriceRangeMax { get; init; }

    public IReadOnlyList<ProductFilterOptionViewModel> Categories { get; init; } = System.Array.Empty<ProductFilterOptionViewModel>();

    public IReadOnlyList<ProductFilterOptionViewModel> DeliveryFormats { get; init; } = System.Array.Empty<ProductFilterOptionViewModel>();

    public IReadOnlyList<ProductFilterOptionViewModel> RatingOptions { get; init; } = System.Array.Empty<ProductFilterOptionViewModel>();

    public IReadOnlyList<ProductFilterOptionViewModel> SortOptions { get; init; } = System.Array.Empty<ProductFilterOptionViewModel>();
}

public class ProductFilterOptionViewModel
{
    public required string Value { get; init; }

    public required string Label { get; init; }

    public int Count { get; init; }

    public bool IsSelected { get; init; }
}
