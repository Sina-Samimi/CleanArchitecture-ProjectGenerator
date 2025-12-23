using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MobiRooz.Domain.Base;

namespace MobiRooz.Domain.Entities.Catalog;

/// <summary>
/// تعریف attribute های قابل انتخاب برای variant ها (مثل "سایز"، "رنگ")
/// </summary>
public sealed class ProductVariantAttribute : Entity
{
    private readonly List<string> _options = new();

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public string Name { get; private set; } // مثل "سایز" یا "رنگ"

    public int DisplayOrder { get; private set; }

    public IReadOnlyCollection<string> Options => _options.AsReadOnly();

    // Private property for EF Core to map Options (stored as comma-separated string)
    // This is accessed via reflection by EF Core
    private string OptionsString
    {
        get => string.Join(',', _options);
        set
        {
            // Load options from string when set by EF Core
            if (!string.IsNullOrWhiteSpace(value))
            {
                LoadOptionsFromString(value);
            }
        }
    }

    [SetsRequiredMembers]
    private ProductVariantAttribute()
    {
        Name = string.Empty;
    }

    [SetsRequiredMembers]
    public ProductVariantAttribute(
        Guid productId,
        string name,
        IEnumerable<string>? options = null,
        int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Attribute name cannot be empty", nameof(name));
        }

        ProductId = productId;
        Name = name.Trim();
        DisplayOrder = displayOrder;
        SetOptions(options);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Attribute name cannot be empty", nameof(name));
        }

        Name = name.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetOptions(IEnumerable<string>? options)
    {
        _options.Clear();

        if (options is null)
        {
            OptionsString = string.Empty;
            UpdateDate = DateTimeOffset.UtcNow;
            return;
        }

        var uniqueOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in options)
        {
            if (string.IsNullOrWhiteSpace(option))
            {
                continue;
            }

            var trimmed = option.Trim();
            if (uniqueOptions.Add(trimmed))
            {
                _options.Add(trimmed);
            }
        }

        UpdateDate = DateTimeOffset.UtcNow;
    }

    // Method to load options from string (called by EF Core)
    internal void LoadOptionsFromString(string? optionsString)
    {
        _options.Clear();
        if (string.IsNullOrWhiteSpace(optionsString))
        {
            return;
        }

        var options = optionsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var option in options)
        {
            if (!string.IsNullOrWhiteSpace(option))
            {
                _options.Add(option);
            }
        }
    }

    public void SetDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
