using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MobiRooz.Domain.Base;
using MobiRooz.Domain.Enums;

namespace MobiRooz.Domain.Entities.Catalog;

public sealed class ProductRequest : Entity
{
    private readonly List<ProductRequestImage> _gallery = new();

    public string Name { get; private set; }

    public string Summary { get; private set; }

    public string Description { get; private set; }

    public ProductType Type { get; private set; }

    public decimal? Price { get; private set; }

    public bool TrackInventory { get; private set; }

    public int StockQuantity { get; private set; }

    public Guid CategoryId { get; private set; }

    public SiteCategory Category { get; private set; } = null!;

    public string? FeaturedImagePath { get; private set; }

    public string TagList { get; private set; }

    public string? DigitalDownloadPath { get; private set; }

    public string SellerId { get; private set; }

    public string? Brand { get; private set; }

    public ProductRequestStatus Status { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public string? ReviewerId { get; private set; }

    public string? RejectionReason { get; private set; }

    public string? SeoTitle { get; private set; }

    public string? SeoDescription { get; private set; }

    public string? SeoKeywords { get; private set; }

    public string SeoSlug { get; private set; }

    public string? Robots { get; private set; }

    public bool IsCustomOrder { get; private set; }

    public Guid? ApprovedProductId { get; private set; }

    public Product? ApprovedProduct { get; private set; }

    // If null: New product request
    // If not null: Offer for existing product
    public Guid? TargetProductId { get; private set; }

    public Product? TargetProduct { get; private set; }

    public IReadOnlyCollection<ProductRequestImage> Gallery => _gallery.AsReadOnly();

    // Helper properties to determine request type
    public bool IsNewProductRequest => TargetProductId is null;
    public bool IsOfferForExistingProduct => TargetProductId.HasValue;

    [SetsRequiredMembers]
    private ProductRequest()
    {
        Name = string.Empty;
        Summary = string.Empty;
        Description = string.Empty;
        TagList = string.Empty;
        SeoSlug = string.Empty;
        SellerId = string.Empty;
    }

    [SetsRequiredMembers]
    public ProductRequest(
        string name,
        string summary,
        string description,
        ProductType type,
        decimal? price,
        bool trackInventory,
        int stockQuantity,
        SiteCategory category,
        string? featuredImagePath,
        IEnumerable<string>? tags,
        string? digitalDownloadPath,
        string sellerId,
        string seoTitle,
        string seoDescription,
        string seoKeywords,
        string seoSlug,
        string? robots,
        bool isCustomOrder = false,
        IEnumerable<(string Path, int Order)>? gallery = null,
        Guid? targetProductId = null,
        string? brand = null)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(sellerId);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name cannot be empty", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Product description cannot be empty", nameof(description));
        }

        Name = name.Trim();
        Summary = (summary ?? string.Empty).Trim();
        Description = description.Trim();
        Type = type;
        Price = price is null ? null : decimal.Round(price.Value, 2, MidpointRounding.AwayFromZero);
        TrackInventory = trackInventory;
        StockQuantity = trackInventory ? stockQuantity : 0;
        CategoryId = category.Id;
        Category = category;
        FeaturedImagePath = string.IsNullOrWhiteSpace(featuredImagePath) ? null : featuredImagePath.Trim();
        SetTags(tags);
        DigitalDownloadPath = string.IsNullOrWhiteSpace(digitalDownloadPath) ? null : digitalDownloadPath.Trim();
        SellerId = sellerId.Trim();
        Status = ProductRequestStatus.Pending;
        SeoTitle = string.IsNullOrWhiteSpace(seoTitle) ? null : seoTitle.Trim();
        SeoDescription = string.IsNullOrWhiteSpace(seoDescription) ? null : seoDescription.Trim();
        SeoKeywords = string.IsNullOrWhiteSpace(seoKeywords) ? null : seoKeywords.Trim();
        SeoSlug = string.IsNullOrWhiteSpace(seoSlug) ? throw new ArgumentException("SEO slug cannot be empty", nameof(seoSlug)) : seoSlug.Trim();
        Robots = string.IsNullOrWhiteSpace(robots) ? null : robots.Trim();
        IsCustomOrder = isCustomOrder;
        TargetProductId = targetProductId;
        Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        ApplyGallery(gallery);
    }

    public void Approve(string reviewerId, Guid approvedProductId)
    {
        if (Status != ProductRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending product requests can be approved.");
        }

        Status = ProductRequestStatus.Approved;
        ReviewedAt = DateTimeOffset.UtcNow;
        ReviewerId = reviewerId;
        ApprovedProductId = approvedProductId;
        RejectionReason = null;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Reject(string reviewerId, string? rejectionReason = null)
    {
        if (Status != ProductRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending product requests can be rejected.");
        }

        Status = ProductRequestStatus.Rejected;
        ReviewedAt = DateTimeOffset.UtcNow;
        ReviewerId = reviewerId;
        RejectionReason = string.IsNullOrWhiteSpace(rejectionReason) ? null : rejectionReason.Trim();
        ApprovedProductId = null;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    private void SetTags(IEnumerable<string>? tags)
    {
        if (tags is null)
        {
            TagList = string.Empty;
            return;
        }

        const int maxTagLength = 50;
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            var normalized = tag.Trim();
            if (normalized.Length > maxTagLength)
            {
                normalized = normalized[..maxTagLength];
            }

            if (unique.Add(normalized))
            {
                ordered.Add(normalized);
            }
        }

        TagList = ordered.Count == 0 ? string.Empty : string.Join(',', ordered);
    }

    private void ApplyGallery(IEnumerable<(string Path, int Order)>? images)
    {
        if (images is null)
        {
            return;
        }

        foreach (var (path, order) in images)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            _gallery.Add(new ProductRequestImage(Id, path.Trim(), order));
        }
    }

    public IReadOnlyCollection<string> GetTags()
    {
        if (string.IsNullOrWhiteSpace(TagList))
        {
            return Array.Empty<string>();
        }

        return TagList
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyCollection<(string Path, int Order)> GetGalleryItems()
    {
        return _gallery
            .OrderBy(img => img.Order)
            .Select(img => (img.Path, img.Order))
            .ToArray();
    }
}

