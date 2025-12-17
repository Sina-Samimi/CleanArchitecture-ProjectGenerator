using System;
using System.Collections.Generic;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed class ProductOfferListViewModel
{
    public IReadOnlyCollection<ProductOfferViewModel> Offers { get; init; } = Array.Empty<ProductOfferViewModel>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public Guid? SelectedProductId { get; init; }
    public string? SelectedSellerId { get; init; }
}

public sealed class ProductOfferViewModel
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductSlug { get; init; }
    public string SellerId { get; init; } = string.Empty;
    public string? SellerName { get; init; }
    public string? SellerPhone { get; init; }
    public decimal? Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public bool TrackInventory { get; init; }
    public int StockQuantity { get; init; }
    public bool IsActive { get; init; }
    public bool IsPublished { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class ProductOfferDetailViewModel
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductSlug { get; init; }
    public string SellerId { get; init; } = string.Empty;
    public string? SellerName { get; init; }
    public string? SellerPhone { get; init; }
    public decimal? Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public bool TrackInventory { get; init; }
    public int StockQuantity { get; init; }
    public bool IsActive { get; init; }
    public bool IsPublished { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public Guid? ApprovedFromRequestId { get; init; }
}

public sealed class ProductOfferFormViewModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public bool TrackInventory { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? PublishedAtPersian { get; set; }
    public string? PublishedAtTime { get; set; }
}

