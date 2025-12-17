using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LogsDtoCloneTest.Domain.Base;
using LogsDtoCloneTest.Domain.Entities.Discounts;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.Domain.Exceptions;
using LogsDtoCloneTest.Domain.Interfaces;

namespace LogsDtoCloneTest.Domain.Entities.Orders;

public sealed class ShoppingCart : Entity, IAggregateRoot
{
    private readonly List<ShoppingCartItem> _items = new();

    public Guid? AnonymousId { get; private set; }

    public string? UserId { get; private set; }

    public string? AppliedDiscountCode { get; private set; }

    public DiscountType? AppliedDiscountType { get; private set; }

    public decimal? AppliedDiscountValue { get; private set; }

    public decimal? AppliedDiscountAmount { get; private set; }

    public bool AppliedDiscountWasCapped { get; private set; }

    public DateTimeOffset? DiscountEvaluatedAt { get; private set; }

    public decimal? DiscountOriginalSubtotal { get; private set; }

    public IReadOnlyCollection<ShoppingCartItem> Items => _items.AsReadOnly();

    public bool IsEmpty => _items.Count == 0;

    public bool HasDiscount => !string.IsNullOrWhiteSpace(AppliedDiscountCode) && AppliedDiscountAmount is not null;

    public decimal Subtotal => decimal.Round(
        _items.Sum(item => item.LineTotal),
        2,
        MidpointRounding.AwayFromZero);

    public decimal DiscountTotal => AppliedDiscountAmount is null
        ? 0m
        : decimal.Round(AppliedDiscountAmount.Value, 2, MidpointRounding.AwayFromZero);

    public decimal GrandTotal
    {
        get
        {
            var total = Subtotal - DiscountTotal;
            return total < 0 ? 0 : decimal.Round(total, 2, MidpointRounding.AwayFromZero);
        }
    }

    [SetsRequiredMembers]
    private ShoppingCart()
    {
    }

    [SetsRequiredMembers]
    private ShoppingCart(Guid? anonymousId, string? userId)
    {
        if (anonymousId is null && string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("Shopping cart must belong to an anonymous identifier or a user.");
        }

        if (anonymousId is not null && anonymousId.Value == Guid.Empty)
        {
            throw new DomainException("Anonymous identifier cannot be empty.");
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            UserId = userId.Trim();
        }

        AnonymousId = anonymousId;
    }

    public static ShoppingCart CreateForAnonymous(Guid anonymousId)
        => new(anonymousId, null);

    public static ShoppingCart CreateForUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("User identifier is required to create a cart.");
        }

        return new ShoppingCart(null, userId.Trim());
    }

    public void AssignAnonymousId(Guid anonymousId)
    {
        if (anonymousId == Guid.Empty)
        {
            throw new DomainException("Anonymous identifier cannot be empty.");
        }

        AnonymousId = anonymousId;
        UserId = null;
        Touch();
    }

    public void AssignToUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("User identifier cannot be empty.");
        }

        UserId = userId.Trim();
        AnonymousId = null;
        Touch();
    }

    public ShoppingCartItem AddItem(
        Guid productId,
        string productName,
        string? productSlug,
        decimal unitPrice,
        decimal? compareAtPrice,
        string? thumbnailPath,
        ProductType productType,
        int quantity,
        Guid? variantId = null,
        Guid? offerId = null)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product identifier cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        // Find existing item with same product, variant, and offer
        var existing = _items.FirstOrDefault(item => 
            item.ProductId == productId && 
            item.VariantId == variantId &&
            item.OfferId == offerId);
        
        if (existing is null)
        {
            var item = new ShoppingCartItem(
                productId,
                productName,
                productSlug,
                unitPrice,
                compareAtPrice,
                thumbnailPath,
                productType,
                quantity,
                variantId,
                offerId);

            _items.Add(item);
            InvalidateDiscount();
            Touch();
            return item;
        }

        existing.UpdateSnapshot(productName, productSlug, unitPrice, compareAtPrice, thumbnailPath, productType);
        existing.IncreaseQuantity(quantity);
        InvalidateDiscount();
        Touch();
        return existing;
    }

    public ShoppingCartItem SetItemQuantity(
        Guid productId,
        string productName,
        string? productSlug,
        decimal unitPrice,
        decimal? compareAtPrice,
        string? thumbnailPath,
        ProductType productType,
        int quantity,
        Guid? variantId = null,
        Guid? offerId = null)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product identifier cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        var existing = _items.FirstOrDefault(item => item.ProductId == productId && item.VariantId == variantId && item.OfferId == offerId)
            ?? throw new DomainException("Item was not found in the cart.");

        existing.UpdateSnapshot(productName, productSlug, unitPrice, compareAtPrice, thumbnailPath, productType);
        existing.ReplaceQuantity(quantity);
        InvalidateDiscount();
        Touch();
        return existing;
    }

    public bool RemoveItem(Guid productId, Guid? variantId = null, Guid? offerId = null)
    {
        var removed = _items.RemoveAll(item =>
            item.ProductId == productId &&
            (variantId is null || item.VariantId == variantId) &&
            (offerId is null || item.OfferId == offerId)) > 0;
        if (removed)
        {
            InvalidateDiscount();
            Touch();
        }

        return removed;
    }

    public void ClearItems()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();
        InvalidateDiscount();
        Touch();
    }

    public void ApplyDiscount(DiscountCode discountCode, DateTimeOffset evaluatedAt, string? audienceKey = null)
    {
        ArgumentNullException.ThrowIfNull(discountCode);

        var subtotal = Subtotal;
        if (subtotal <= 0)
        {
            throw new DomainException("Cannot apply a discount to an empty cart.");
        }

        var result = discountCode.Preview(subtotal, evaluatedAt, audienceKey);

        AppliedDiscountCode = result.Code;
        AppliedDiscountType = result.AppliedDiscountType;
        AppliedDiscountValue = result.AppliedDiscountValue;
        AppliedDiscountAmount = result.DiscountAmount;
        AppliedDiscountWasCapped = result.WasCapped;
        DiscountEvaluatedAt = result.EvaluatedAt;
        DiscountOriginalSubtotal = result.OriginalPrice;

        Touch();
    }

    public void ClearDiscount()
    {
        if (!HasDiscount)
        {
            return;
        }

        AppliedDiscountCode = null;
        AppliedDiscountType = null;
        AppliedDiscountValue = null;
        AppliedDiscountAmount = null;
        AppliedDiscountWasCapped = false;
        DiscountEvaluatedAt = null;
        DiscountOriginalSubtotal = null;
        Touch();
    }

    public void MergeFrom(ShoppingCart source)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source._items)
        {
            AddItem(
                item.ProductId,
                item.ProductName,
                item.ProductSlug,
                item.UnitPrice,
                item.CompareAtPrice,
                item.ThumbnailPath,
                item.ProductType,
                item.Quantity);
        }
    }

    public void EnsureDiscountMatchesSubtotal()
    {
        if (!HasDiscount)
        {
            return;
        }

        if (DiscountOriginalSubtotal is null || DiscountOriginalSubtotal.Value != Subtotal)
        {
            ClearDiscount();
        }
    }

    private void InvalidateDiscount()
    {
        AppliedDiscountCode = null;
        AppliedDiscountType = null;
        AppliedDiscountValue = null;
        AppliedDiscountAmount = null;
        AppliedDiscountWasCapped = false;
        DiscountEvaluatedAt = null;
        DiscountOriginalSubtotal = null;
    }

    private void Touch()
    {
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
