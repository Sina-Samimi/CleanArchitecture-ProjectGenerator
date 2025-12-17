using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.WebSite.Models.Product;

namespace LogsDtoCloneTest.WebSite.Services.Products;

public interface IProductCatalogService
{
    Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(int count, CancellationToken cancellationToken = default);

    Task<ProductListResult> GetProductsAsync(ProductFilterOptions filterOptions, CancellationToken cancellationToken = default);

    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetRelatedProductsAsync(Guid productId, int count, CancellationToken cancellationToken = default);

    Task<bool> AddCommentAsync(
        Guid productId,
        string authorName,
        string content,
        double rating,
        Guid? parentId = null,
        CancellationToken cancellationToken = default);
}

public record ProductFilterOptions(
    string? SearchTerm,
    string? Category,
    string? DeliveryFormat,
    decimal? MinPrice,
    decimal? MaxPrice,
    double? MinRating,
    string? SortBy
);

public record ProductListResult(
    IReadOnlyList<Product> Products,
    int TotalCount,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> DeliveryFormats,
    decimal PriceRangeMin,
    decimal PriceRangeMax
);
