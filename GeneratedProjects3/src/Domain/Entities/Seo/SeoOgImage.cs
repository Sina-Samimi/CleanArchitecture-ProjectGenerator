using System;
using System.Diagnostics.CodeAnalysis;
using LogTableRenameTest.Domain.Base;

namespace LogTableRenameTest.Domain.Entities.Seo;

public sealed class SeoOgImage : Entity
{
    public Guid SeoMetadataId { get; private set; }

    public SeoMetadata SeoMetadata { get; private set; } = null!;

    public string ImageUrl { get; private set; } = string.Empty;

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    public string? ImageType { get; private set; } // png, jpg, webp, etc.

    public string? Alt { get; private set; }

    public int DisplayOrder { get; private set; }

    [SetsRequiredMembers]
    private SeoOgImage()
    {
    }

    [SetsRequiredMembers]
    public SeoOgImage(
        Guid seoMetadataId,
        string imageUrl,
        int displayOrder,
        int? width = null,
        int? height = null,
        string? imageType = null,
        string? alt = null)
    {
        UpdateDetails(seoMetadataId, imageUrl, displayOrder, width, height, imageType, alt);
    }

    public void UpdateDetails(
        Guid seoMetadataId,
        string imageUrl,
        int displayOrder,
        int? width = null,
        int? height = null,
        string? imageType = null,
        string? alt = null)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("Image URL cannot be empty", nameof(imageUrl));
        }

        SeoMetadataId = seoMetadataId;
        ImageUrl = imageUrl.Trim();
        DisplayOrder = Math.Max(0, displayOrder);
        Width = width.HasValue && width.Value > 0 ? width : null;
        Height = height.HasValue && height.Value > 0 ? height : null;
        ImageType = string.IsNullOrWhiteSpace(imageType) ? null : imageType.Trim();
        Alt = string.IsNullOrWhiteSpace(alt) ? null : alt.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateOrder(int displayOrder)
    {
        DisplayOrder = Math.Max(0, displayOrder);
        UpdateDate = DateTimeOffset.UtcNow;
    }
}

