using System;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;
using Arsis.Domain.Enums;
using Arsis.Domain.Exceptions;

namespace Arsis.Domain.Entities.Orders;

public sealed class ShoppingCartItem : Entity
{
    [SetsRequiredMembers]
    private ShoppingCartItem()
    {
        ProductName = string.Empty;
        ProductSlug = string.Empty;
    }

    [SetsRequiredMembers]
    public ShoppingCartItem(
        Guid productId,
        string productName,
        string? productSlug,
        decimal unitPrice,
        decimal? compareAtPrice,
        string? thumbnailPath,
        ProductType productType,
        int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product identifier cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        ProductId = productId;
        ProductType = productType;
        UpdateSnapshot(productName, productSlug, unitPrice, compareAtPrice, thumbnailPath, productType);
        Quantity = quantity;
    }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; }

    public string ProductSlug { get; private set; }

    public string? ThumbnailPath { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal? CompareAtPrice { get; private set; }

    public int Quantity { get; private set; }

    public ProductType ProductType { get; private set; }

    public Guid CartId { get; private set; }

    public ShoppingCart Cart { get; private set; } = null!;

    public decimal LineTotal => decimal.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);

    public void UpdateSnapshot(
        string productName,
        string? productSlug,
        decimal unitPrice,
        decimal? compareAtPrice,
        string? thumbnailPath,
        ProductType productType)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new DomainException("Product name is required.");
        }

        ProductName = productName.Trim();
        ProductSlug = string.IsNullOrWhiteSpace(productSlug) ? string.Empty : productSlug.Trim();
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
        CompareAtPrice = compareAtPrice is null
            ? null
            : decimal.Round(compareAtPrice.Value, 2, MidpointRounding.AwayFromZero);
        ThumbnailPath = string.IsNullOrWhiteSpace(thumbnailPath) ? null : thumbnailPath.Trim();
        ProductType = productType;
    }

    public void IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity increment must be greater than zero.");
        }

        Quantity += quantity;

        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void ReplaceQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        Quantity = quantity;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
