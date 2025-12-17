using System.Collections.Generic;

namespace TestAttarClone.WebSite.Models.Product;

public class ProductListViewModel
{
    public IReadOnlyList<ProductSummaryViewModel> Products { get; init; } = System.Array.Empty<ProductSummaryViewModel>();

    public required ProductFilterViewModel Filters { get; init; }

    public int TotalCount { get; init; }

    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(Filters.SearchTerm)
        || !string.IsNullOrWhiteSpace(Filters.SelectedCategory)
        || !string.IsNullOrWhiteSpace(Filters.SelectedDeliveryFormat)
        || Filters.MinRating.HasValue
        || Filters.MinPrice.HasValue
        || Filters.MaxPrice.HasValue;
}
