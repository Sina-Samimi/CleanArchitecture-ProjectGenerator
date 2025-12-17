using System;
using System.Diagnostics.CodeAnalysis;
using Attar.Domain.Base;

namespace Attar.Domain.Entities.Catalog;

public sealed class ProductAttribute : Entity
{
    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public string Key { get; private set; }

    public string Value { get; private set; }

    public int DisplayOrder { get; private set; }

    [SetsRequiredMembers]
    private ProductAttribute()
    {
        Key = string.Empty;
        Value = string.Empty;
    }

    [SetsRequiredMembers]
    public ProductAttribute(
        Guid productId,
        string key,
        string value,
        int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Attribute key cannot be empty", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Attribute value cannot be empty", nameof(value));
        }

        ProductId = productId;
        Key = key.Trim();
        Value = value.Trim();
        DisplayOrder = displayOrder;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Attribute key cannot be empty", nameof(key));
        }

        Key = key.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Attribute value cannot be empty", nameof(value));
        }

        Value = value.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateContent(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Attribute key cannot be empty", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Attribute value cannot be empty", nameof(value));
        }

        Key = key.Trim();
        Value = value.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}

