using System;
using System.Collections.Generic;
using Attar.Domain.Enums;

namespace Attar.Application.DTOs.Catalog;

public sealed record ProductListItemDto(
    Guid Id,
    string Name,
    ProductType Type,
    decimal? Price,
    decimal? CompareAtPrice,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    Guid CategoryId,
    string CategoryName,
    string? FeaturedImagePath,
    string TagList,
    DateTimeOffset UpdatedAt,
    bool IsCustomOrder,
    string? SellerId,
    string? SellerName,
    string? SellerPhone,
    int SellerCount);

public sealed record ProductGalleryImageDto(
    Guid Id,
    string ImagePath,
    int DisplayOrder);

public sealed record ProductVariantAttributeDto(
    Guid Id,
    string Name,
    IReadOnlyCollection<string> Options,
    int DisplayOrder);

public sealed record ProductVariantOptionDto(
    Guid Id,
    Guid VariantAttributeId,
    string Value);

public sealed record ProductVariantDto(
    Guid Id,
    decimal? Price,
    decimal? CompareAtPrice,
    int StockQuantity,
    string? Sku,
    string? ImagePath,
    bool IsActive,
    IReadOnlyCollection<ProductVariantOptionDto> Options);

public sealed record ProductDetailDto(
    Guid Id,
    string Name,
    string Summary,
    string Description,
    ProductType Type,
    decimal? Price,
    decimal? CompareAtPrice,
    bool TrackInventory,
    int StockQuantity,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    Guid CategoryId,
    string CategoryName,
    string? Brand,
    string SeoTitle,
    string SeoDescription,
    string SeoKeywords,
    string SeoSlug,
    string Robots,
    string TagList,
    string? FeaturedImagePath,
    string? DigitalDownloadPath,
    string? SellerId,
    IReadOnlyCollection<ProductGalleryImageDto> Gallery,
    int ViewCount,
    bool IsCustomOrder,
    IReadOnlyCollection<ProductVariantAttributeDto> VariantAttributes,
    IReadOnlyCollection<ProductVariantDto> Variants);

public sealed record ProductSalesTrendPointDto(
    DateTimeOffset PeriodStart,
    decimal Quantity,
    decimal Revenue);

public sealed record ProductSalesSummaryDto(
    Guid ProductId,
    int TotalOrders,
    decimal TotalQuantity,
    decimal TotalRevenue,
    decimal TotalDiscount,
    decimal AverageOrderValue,
    DateTimeOffset? FirstSaleAt,
    DateTimeOffset? LastSaleAt,
    IReadOnlyCollection<ProductSalesTrendPointDto> Trend);

public sealed record SellerProductDetailDto(
    Guid Id,
    string Name,
    string Summary,
    string Description,
    ProductType Type,
    decimal? Price,
    decimal? CompareAtPrice,
    bool TrackInventory,
    int StockQuantity,
    Guid CategoryId,
    string CategoryName,
    string? Brand,
    string TagList,
    bool IsCustomOrder,
    string? FeaturedImagePath,
    string? DigitalDownloadPath,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<ProductGalleryImageDto> Gallery,
    int ViewCount);

public sealed record ProductListResultDto(
    IReadOnlyCollection<ProductListItemDto> Items,
    int TotalCount,
    int FilteredCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

public sealed record SiteCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    CategoryScope Scope,
    Guid? ParentId,
    int Depth,
    IReadOnlyCollection<SiteCategoryDto> Children);

public sealed record ProductLookupsDto(
    IReadOnlyCollection<SiteCategoryDto> Categories,
    IReadOnlyCollection<string> Tags);

public sealed record ProductExecutionStepDto(
    Guid Id,
    string Title,
    string? Description,
    string? Duration,
    int DisplayOrder);

public sealed record ProductExecutionStepsDto(
    Guid ProductId,
    string ProductName,
    IReadOnlyCollection<ProductExecutionStepDto> Steps);

public sealed record ProductExecutionStepSummaryDto(
    Guid ProductId,
    string ProductName,
    string CategoryName,
    ProductType Type,
    bool IsPublished,
    int StepCount,
    DateTimeOffset? UpdatedAt);

public sealed record ProductExecutionStepsOverviewDto(
    int TotalProducts,
    int ProductsWithSteps,
    int TotalSteps,
    double AverageStepsPerProduct,
    IReadOnlyCollection<ProductExecutionStepSummaryDto> Items);

public sealed record ProductFaqDto(
    Guid Id,
    string Question,
    string Answer,
    int DisplayOrder);

public sealed record ProductFaqsDto(
    Guid ProductId,
    string ProductName,
    IReadOnlyCollection<ProductFaqDto> Items);

public sealed record ProductSummaryDto(
    Guid Id,
    string Name,
    string SeoSlug);

public sealed record ProductCommentDto(
    Guid Id,
    Guid ProductId,
    Guid? ParentId,
    string AuthorName,
    string Content,
    double Rating,
    bool IsApproved,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? ApprovedById,
    string? ApprovedByName,
    DateTimeOffset? ApprovedAt);

public sealed record ProductCommentListResultDto(
    ProductSummaryDto Product,
    IReadOnlyCollection<ProductCommentDto> Comments);

public sealed record PendingProductCommentDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductSlug,
    string AuthorName,
    string Excerpt,
    DateTimeOffset CreatedAt);

public sealed record PendingProductCommentListResultDto(
    IReadOnlyCollection<PendingProductCommentDto> Items,
    int TotalCount);

public sealed record ProductAttributeDto(
    Guid Id,
    string Key,
    string Value,
    int DisplayOrder);

public sealed record ProductAttributesDto(
    Guid ProductId,
    string ProductName,
    IReadOnlyCollection<ProductAttributeDto> Items);
