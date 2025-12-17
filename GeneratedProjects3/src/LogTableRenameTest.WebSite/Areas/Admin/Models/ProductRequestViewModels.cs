using System;
using System.Collections.Generic;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.WebSite.Areas.Admin.Models;

public sealed class ProductRequestListViewModel
{
    public IReadOnlyCollection<ProductRequestViewModel> Requests { get; init; }
        = Array.Empty<ProductRequestViewModel>();

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public ProductRequestStatus? SelectedStatus { get; init; }

    public string? SelectedSellerId { get; init; }
}

public sealed class ProductRequestViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public ProductType Type { get; init; }

    public decimal? Price { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public string? FeaturedImagePath { get; init; }

    public string SellerId { get; init; } = string.Empty;

    public string? SellerName { get; init; }

    public string? SellerPhone { get; init; }

    public ProductRequestStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ReviewedAt { get; init; }

    public string? RejectionReason { get; init; }

    public Guid? ApprovedProductId { get; init; }

    public bool IsCustomOrder { get; init; }
}

public sealed class ProductRequestDetailViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public ProductType Type { get; init; }

    public decimal? Price { get; init; }

    public bool TrackInventory { get; init; }

    public int StockQuantity { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public string? FeaturedImagePath { get; init; }

    public string? DigitalDownloadPath { get; init; }

    public string TagList { get; init; } = string.Empty;

    public string SellerId { get; init; } = string.Empty;

    public string? SellerName { get; init; }

    public string? SellerPhone { get; init; }

    public ProductRequestStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public DateTimeOffset? ReviewedAt { get; init; }

    public string? ReviewerId { get; init; }

    public string? ReviewerName { get; init; }

    public string? RejectionReason { get; init; }

    public Guid? ApprovedProductId { get; init; }

    public string SeoSlug { get; init; } = string.Empty;

    public bool IsCustomOrder { get; init; }

    public IReadOnlyCollection<ProductRequestGalleryImageViewModel> Gallery { get; init; }
        = Array.Empty<ProductRequestGalleryImageViewModel>();
}

public sealed class ProductRequestGalleryImageViewModel
{
    public Guid Id { get; init; }

    public string Path { get; init; } = string.Empty;

    public int Order { get; init; }
}

