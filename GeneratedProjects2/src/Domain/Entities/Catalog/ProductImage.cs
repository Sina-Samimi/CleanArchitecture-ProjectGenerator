using System;
using System.Diagnostics.CodeAnalysis;
using LogsDtoCloneTest.Domain.Base;

namespace LogsDtoCloneTest.Domain.Entities.Catalog;

public sealed class ProductImage : Entity
{
    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public string ImagePath { get; private set; }

    public int DisplayOrder { get; private set; }

    [SetsRequiredMembers]
    private ProductImage()
    {
        ImagePath = string.Empty;
    }

    [SetsRequiredMembers]
    public ProductImage(Guid productId, string imagePath, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("Image path cannot be empty", nameof(imagePath));
        }

        ProductId = productId;
        ImagePath = imagePath.Trim();
        DisplayOrder = Math.Max(0, displayOrder);
    }

    public void UpdateImage(string imagePath, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("Image path cannot be empty", nameof(imagePath));
        }

        ImagePath = imagePath.Trim();
        DisplayOrder = Math.Max(0, displayOrder);
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
