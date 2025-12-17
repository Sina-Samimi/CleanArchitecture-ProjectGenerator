using System;
using System.Diagnostics.CodeAnalysis;
using Attar.Domain.Base;

namespace Attar.Domain.Entities.Catalog;

/// <summary>
/// هر option یک مقدار برای یک variant attribute است (مثلاً "سایز: M" یا "رنگ: قرمز")
/// </summary>
public sealed class ProductVariantOption : Entity
{
    public Guid VariantId { get; private set; }

    public ProductVariant Variant { get; private set; } = null!;

    public Guid VariantAttributeId { get; private set; } // ارجاع به ProductVariantAttribute

    public string Value { get; private set; } // مقدار انتخاب شده (مثلاً "M" یا "قرمز")

    [SetsRequiredMembers]
    private ProductVariantOption()
    {
        Value = string.Empty;
    }

    [SetsRequiredMembers]
    internal ProductVariantOption(
        Guid variantId,
        Guid variantAttributeId,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Option value cannot be empty", nameof(value));
        }

        VariantId = variantId;
        VariantAttributeId = variantAttributeId;
        Value = value.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Option value cannot be empty", nameof(value));
        }

        Value = value.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
