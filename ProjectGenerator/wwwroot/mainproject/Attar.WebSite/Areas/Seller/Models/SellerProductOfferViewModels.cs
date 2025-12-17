using System;
using System.Collections.Generic;

namespace Attar.WebSite.Areas.Seller.Models;

public sealed class SellerProductOfferListViewModel
{
    public IReadOnlyCollection<SellerProductOfferViewModel> Offers { get; init; } = Array.Empty<SellerProductOfferViewModel>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}

public sealed class SellerProductOfferViewModel
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductSlug { get; init; }
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

public sealed class SellerProductOfferDetailViewModel
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductSlug { get; init; }
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

public sealed class SellerProductOfferFormViewModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public bool TrackInventory { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateProductOfferRequestViewModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public bool TrackInventory { get; set; }
    public int StockQuantity { get; set; }
}

