using System;
using System.Collections.Generic;
using Arsis.Domain.Enums;

namespace Arsis.Application.DTOs.Catalog;

public sealed record ProductListItemDto(
    Guid Id,
    string Name,
    ProductType Type,
    decimal Price,
    decimal? CompareAtPrice,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    Guid CategoryId,
    string CategoryName,
    string? FeaturedImagePath,
    string TagList,
    DateTimeOffset UpdatedAt);

public sealed record ProductGalleryImageDto(
    Guid Id,
    string ImagePath,
    int DisplayOrder);

public sealed record ProductDetailDto(
    Guid Id,
    string Name,
    string Summary,
    string Description,
    ProductType Type,
    decimal Price,
    decimal? CompareAtPrice,
    bool TrackInventory,
    int StockQuantity,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    Guid CategoryId,
    string CategoryName,
    string SeoTitle,
    string SeoDescription,
    string SeoKeywords,
    string SeoSlug,
    string Robots,
    string TagList,
    string? FeaturedImagePath,
    string? DigitalDownloadPath,
    string? TeacherId,
    IReadOnlyCollection<ProductGalleryImageDto> Gallery);

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

public sealed record TeacherProductDetailDto(
    Guid Id,
    string Name,
    string Summary,
    string Description,
    ProductType Type,
    decimal Price,
    decimal? CompareAtPrice,
    bool TrackInventory,
    int StockQuantity,
    Guid CategoryId,
    string CategoryName,
    string TagList,
    string? FeaturedImagePath,
    string? DigitalDownloadPath,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<ProductGalleryImageDto> Gallery);

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
