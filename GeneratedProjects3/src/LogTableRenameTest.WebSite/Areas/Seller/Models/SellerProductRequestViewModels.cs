using System;
using System.Collections.Generic;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.WebSite.Areas.Seller.Models;

public sealed class SellerProductRequestListViewModel
{
    public IReadOnlyCollection<SellerProductRequestViewModel> Requests { get; init; }
        = Array.Empty<SellerProductRequestViewModel>();

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public ProductRequestStatus? SelectedStatus { get; init; }
}

public sealed class SellerProductRequestViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public ProductType Type { get; init; }

    public decimal? Price { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public string? FeaturedImagePath { get; init; }

    public ProductRequestStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ReviewedAt { get; init; }

    public string? RejectionReason { get; init; }

    public Guid? ApprovedProductId { get; init; }

    public bool IsCustomOrder { get; init; }
}

public sealed class SellerProductRequestDetailViewModel
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

    public ProductRequestStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public DateTimeOffset? ReviewedAt { get; init; }

    public string? RejectionReason { get; init; }

    public Guid? ApprovedProductId { get; init; }

    public string SeoSlug { get; init; } = string.Empty;

    public bool IsCustomOrder { get; init; }

    public IReadOnlyCollection<SellerProductRequestGalleryImageViewModel> Gallery { get; init; }
        = Array.Empty<SellerProductRequestGalleryImageViewModel>();
}

public sealed class SellerProductRequestGalleryImageViewModel
{
    public Guid Id { get; init; }

    public string Path { get; init; } = string.Empty;

    public int Order { get; init; }
}

