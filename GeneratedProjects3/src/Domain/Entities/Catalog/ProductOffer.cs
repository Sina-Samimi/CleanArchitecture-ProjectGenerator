using System;
using System.Diagnostics.CodeAnalysis;
using LogTableRenameTest.Domain.Base;
using LogTableRenameTest.Domain.Entities.Sellers;

namespace LogTableRenameTest.Domain.Entities.Catalog;

public sealed class ProductOffer : Entity
{
    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public string SellerId { get; private set; }

    public decimal? Price { get; private set; }

    public decimal? CompareAtPrice { get; private set; }

    public bool TrackInventory { get; private set; }

    public int StockQuantity { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    // Link to the ProductRequest that was approved to create this offer
    public Guid? ApprovedFromRequestId { get; private set; }

    public ProductRequest? ApprovedFromRequest { get; private set; }

    [SetsRequiredMembers]
    private ProductOffer()
    {
        SellerId = string.Empty;
    }

    [SetsRequiredMembers]
    public ProductOffer(
        Guid productId,
        string sellerId,
        decimal? price,
        bool trackInventory,
        int stockQuantity,
        decimal? compareAtPrice = null,
        bool isActive = true,
        bool isPublished = false,
        Guid? approvedFromRequestId = null)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(sellerId))
        {
            throw new ArgumentException("Seller ID cannot be empty", nameof(sellerId));
        }

        if (price.HasValue && price.Value < 0)
        {
            throw new ArgumentException("Price cannot be negative", nameof(price));
        }

        if (trackInventory && stockQuantity < 0)
        {
            throw new ArgumentException("Stock quantity cannot be negative when tracking inventory", nameof(stockQuantity));
        }

        ProductId = productId;
        SellerId = sellerId.Trim();
        Price = price is null ? null : decimal.Round(price.Value, 2, MidpointRounding.AwayFromZero);
        CompareAtPrice = compareAtPrice is null ? null : decimal.Round(compareAtPrice.Value, 2, MidpointRounding.AwayFromZero);
        TrackInventory = trackInventory;
        StockQuantity = trackInventory ? stockQuantity : 0;
        IsActive = isActive;
        IsPublished = isPublished;
        ApprovedFromRequestId = approvedFromRequestId;

        if (isPublished)
        {
            PublishedAt = DateTimeOffset.UtcNow;
        }
    }

    public void UpdatePricing(decimal? price, decimal? compareAtPrice = null)
    {
        if (price.HasValue && price.Value < 0)
        {
            throw new ArgumentException("Price cannot be negative", nameof(price));
        }

        if (compareAtPrice.HasValue && compareAtPrice.Value < 0)
        {
            throw new ArgumentException("Compare at price cannot be negative", nameof(compareAtPrice));
        }

        Price = price is null ? null : decimal.Round(price.Value, 2, MidpointRounding.AwayFromZero);
        CompareAtPrice = compareAtPrice is null ? null : decimal.Round(compareAtPrice.Value, 2, MidpointRounding.AwayFromZero);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateInventory(bool trackInventory, int stockQuantity)
    {
        if (trackInventory && stockQuantity < 0)
        {
            throw new ArgumentException("Stock quantity cannot be negative when tracking inventory", nameof(stockQuantity));
        }

        TrackInventory = trackInventory;
        StockQuantity = trackInventory ? stockQuantity : 0;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Publish(DateTimeOffset? publishedAt = null)
    {
        IsPublished = true;
        PublishedAt = publishedAt ?? DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Unpublish()
    {
        IsPublished = false;
        PublishedAt = null;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void ReduceStock(int quantity)
    {
        if (!TrackInventory)
        {
            return;
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        }

        if (StockQuantity < quantity)
        {
            throw new InvalidOperationException("Insufficient stock quantity");
        }

        StockQuantity -= quantity;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void IncreaseStock(int quantity)
    {
        if (!TrackInventory)
        {
            return;
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        }

        StockQuantity += quantity;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
