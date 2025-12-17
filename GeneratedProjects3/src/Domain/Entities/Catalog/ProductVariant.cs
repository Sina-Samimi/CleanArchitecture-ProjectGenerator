using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LogTableRenameTest.Domain.Base;

namespace LogTableRenameTest.Domain.Entities.Catalog;

/// <summary>
/// هر variant یک ترکیب از option ها است (مثلاً: سایز M + رنگ قرمز)
/// </summary>
public sealed class ProductVariant : Entity
{
    private readonly List<ProductVariantOption> _options = new();

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public decimal? Price { get; private set; } // قیمت این variant (اختیاری - اگر null باشد از قیمت محصول استفاده می‌شود)

    public decimal? CompareAtPrice { get; private set; }

    public int StockQuantity { get; private set; }

    public string? Sku { get; private set; }

    public string? ImagePath { get; private set; } // تصویر مخصوص این variant

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<ProductVariantOption> Options => _options.AsReadOnly();

    [SetsRequiredMembers]
    private ProductVariant()
    {
    }

    [SetsRequiredMembers]
    public ProductVariant(
        Guid productId,
        decimal? price = null,
        decimal? compareAtPrice = null,
        int stockQuantity = 0,
        string? sku = null,
        string? imagePath = null,
        bool isActive = true)
    {
        ProductId = productId;
        SetPricing(price, compareAtPrice);
        SetStockQuantity(stockQuantity);
        SetSku(sku);
        SetImagePath(imagePath);
        IsActive = isActive;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetPricing(decimal? price, decimal? compareAtPrice = null)
    {
        if (price is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price));
        }

        if (compareAtPrice is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(compareAtPrice));
        }

        Price = price is null
            ? null
            : decimal.Round(price.Value, 2, MidpointRounding.AwayFromZero);
        CompareAtPrice = compareAtPrice is null
            ? null
            : decimal.Round(compareAtPrice.Value, 2, MidpointRounding.AwayFromZero);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetStockQuantity(int stockQuantity)
    {
        if (stockQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stockQuantity));
        }

        StockQuantity = stockQuantity;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (StockQuantity < quantity)
        {
            throw new InvalidOperationException($"Cannot reduce variant stock by {quantity}. Only {StockQuantity} items available.");
        }

        StockQuantity -= quantity;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetSku(string? sku)
    {
        Sku = string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetImagePath(string? imagePath)
    {
        ImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public ProductVariantOption AddOption(Guid variantAttributeId, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Option value cannot be empty", nameof(value));
        }

        var option = new ProductVariantOption(Id, variantAttributeId, value.Trim());
        _options.Add(option);
        UpdateDate = DateTimeOffset.UtcNow;
        return option;
    }

    public void SetOptions(IEnumerable<(Guid VariantAttributeId, string Value)> options)
    {
        _options.Clear();

        if (options is null)
        {
            UpdateDate = DateTimeOffset.UtcNow;
            return;
        }

        foreach (var (variantAttributeId, value) in options)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            _options.Add(new ProductVariantOption(Id, variantAttributeId, value.Trim()));
        }

        UpdateDate = DateTimeOffset.UtcNow;
    }

    public bool RemoveOption(Guid optionId)
    {
        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option is null)
        {
            return false;
        }

        _options.Remove(option);
        UpdateDate = DateTimeOffset.UtcNow;
        return true;
    }

    /// <summary>
    /// بررسی می‌کند که آیا این variant با variant دیگر یکسان است (همان option ها را دارد)
    /// </summary>
    public bool MatchesOptions(IEnumerable<(Guid VariantAttributeId, string Value)> options)
    {
        if (options is null)
        {
            return _options.Count == 0;
        }

        var optionsList = options.ToList();
        if (_options.Count != optionsList.Count)
        {
            return false;
        }

        foreach (var (variantAttributeId, value) in optionsList)
        {
            var option = _options.FirstOrDefault(o =>
                o.VariantAttributeId == variantAttributeId &&
                string.Equals(o.Value, value, StringComparison.OrdinalIgnoreCase));

            if (option is null)
            {
                return false;
            }
        }

        return true;
    }
}
