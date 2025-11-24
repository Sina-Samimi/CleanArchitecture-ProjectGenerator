using System;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;

namespace Arsis.Domain.Entities.Catalog;

public sealed class ProductExecutionStep : Entity
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 1000;
    private const int MaxDurationLength = 100;

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public string? Duration { get; private set; }

    public int DisplayOrder { get; private set; }

    [SetsRequiredMembers]
    private ProductExecutionStep()
    {
        Title = string.Empty;
    }

    [SetsRequiredMembers]
    public ProductExecutionStep(Guid productId, string title, string? description, string? duration, int displayOrder)
    {
        ProductId = productId;
        UpdateDetails(title, description, duration, displayOrder);
    }

    public void UpdateDetails(string title, string? description, string? duration, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Step title cannot be empty", nameof(title));
        }

        Title = Normalize(title, MaxTitleLength);
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : Normalize(description, MaxDescriptionLength);
        Duration = string.IsNullOrWhiteSpace(duration)
            ? null
            : Normalize(duration, MaxDurationLength);
        DisplayOrder = Math.Max(0, displayOrder);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateOrder(int displayOrder)
    {
        DisplayOrder = Math.Max(0, displayOrder);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    private static string Normalize(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
