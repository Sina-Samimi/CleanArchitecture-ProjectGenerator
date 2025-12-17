using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LogTableRenameTest.Domain.Base;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.Domain.Entities.Catalog;

public sealed class Product : SeoEntity
{
    private const int MaxTagLength = 50;

    private readonly List<ProductImage> _gallery = new();
    private readonly List<ProductExecutionStep> _executionSteps = new();
    private readonly List<ProductFaq> _faqs = new();
    private readonly List<ProductComment> _comments = new();
    private readonly List<ProductAttribute> _attributes = new();
    private readonly List<ProductVariantAttribute> _variantAttributes = new();
    private readonly List<ProductVariant> _variants = new();

    public string Name { get; private set; }

    public string Summary { get; private set; }

    public string Description { get; private set; }

    public ProductType Type { get; private set; }

    public decimal? Price { get; private set; }

    public decimal? CompareAtPrice { get; private set; }

    public bool IsCustomOrder { get; private set; }

    public bool TrackInventory { get; private set; }

    public int StockQuantity { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public Guid CategoryId { get; private set; }

    public SiteCategory Category { get; private set; } = null!;

    public string? FeaturedImagePath { get; private set; }

    public string TagList { get; private set; }

    public string? DigitalDownloadPath { get; private set; }

    public string? SellerId { get; private set; }

    public string? Brand { get; private set; }

    public IReadOnlyCollection<ProductImage> Gallery => _gallery.AsReadOnly();

    public IReadOnlyCollection<string> Tags => ParseTags(TagList);

    public IReadOnlyCollection<ProductExecutionStep> ExecutionSteps => _executionSteps.AsReadOnly();

    public IReadOnlyCollection<ProductFaq> Faqs => _faqs.AsReadOnly();

    public IReadOnlyCollection<ProductComment> Comments => _comments.AsReadOnly();

    public IReadOnlyCollection<ProductAttribute> Attributes => _attributes.AsReadOnly();

    public IReadOnlyCollection<ProductVariantAttribute> VariantAttributes => _variantAttributes.AsReadOnly();

    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    public bool HasVariants => _variants.Count > 0 && _variantAttributes.Count > 0;

    [SetsRequiredMembers]
    private Product()
    {
        Name = string.Empty;
        Summary = string.Empty;
        Description = string.Empty;
        TagList = string.Empty;
    }

    [SetsRequiredMembers]
    public Product(
        string name,
        string summary,
        string description,
        ProductType type,
        decimal? price,
        decimal? compareAtPrice,
        bool trackInventory,
        int stockQuantity,
        SiteCategory category,
        string seoTitle,
        string seoDescription,
        string seoKeywords,
        string seoSlug,
        string? robots,
        string? featuredImagePath,
        IEnumerable<string>? tags,
        string? digitalDownloadPath = null,
        bool isPublished = false,
        DateTimeOffset? publishedAt = null,
        IEnumerable<(string Path, int Order)>? gallery = null,
        string? sellerId = null,
        bool isCustomOrder = false,
        string? brand = null)
    {
        ArgumentNullException.ThrowIfNull(category);

        UpdateContent(name, summary, description);
        ChangeType(type, digitalDownloadPath);
        UpdatePricing(price, compareAtPrice);
        UpdateInventory(trackInventory, stockQuantity);
        SetCategory(category);
        SetFeaturedImage(featuredImagePath);
        SetTags(tags);
        UpdateSeo(seoTitle, seoDescription, seoKeywords, seoSlug, robots);
        ApplyGallery(gallery);
        AssignSeller(sellerId);
        SetCustomOrder(isCustomOrder);
        SetBrand(brand);

        if (isPublished)
        {
            Publish(publishedAt);
        }
        else
        {
            Unpublish();
        }
    }

    public void UpdateContent(string name, string summary, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name cannot be empty", nameof(name));
        }

        Name = name.Trim();
        Summary = (summary ?? string.Empty).Trim();
        Description = (description ?? string.Empty).Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void ChangeType(ProductType type, string? digitalDownloadPath = null)
    {
        Type = type;
        if (type == ProductType.Digital)
        {
            SetDigitalDownload(digitalDownloadPath);
            TrackInventory = false;
            StockQuantity = 0;
        }
        else
        {
            DigitalDownloadPath = null;
        }

        UpdateDate = DateTimeOffset.UtcNow;
    }

    public ProductComment AddComment(
        string authorName,
        string content,
        double rating,
        Guid? parentId = null,
        bool isApproved = false)
    {
        ProductComment? parent = null;
        if (parentId.HasValue)
        {
            parent = _comments.FirstOrDefault(comment => comment.Id == parentId.Value);
            if (parent is null)
            {
                throw new InvalidOperationException("Parent comment could not be found for the provided identifier.");
            }
        }

        var comment = new ProductComment(Id, authorName, content, rating, parent, isApproved);
        _comments.Add(comment);
        UpdateDate = DateTimeOffset.UtcNow;
        return comment;
    }

    public void UpdatePricing(decimal? price, decimal? compareAtPrice)
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

    public void SetCustomOrder(bool isCustomOrder)
    {
        IsCustomOrder = isCustomOrder;
        if (isCustomOrder)
        {
            Price = null;
            CompareAtPrice = null;
        }
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateInventory(bool trackInventory, int stockQuantity)
    {
        if (trackInventory && Type == ProductType.Digital)
        {
            trackInventory = false;
            stockQuantity = 0;
        }

        if (stockQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stockQuantity));
        }

        TrackInventory = trackInventory;
        StockQuantity = trackInventory ? stockQuantity : 0;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (!TrackInventory)
        {
            return; // No inventory tracking, nothing to reduce
        }

        if (StockQuantity < quantity)
        {
            throw new InvalidOperationException($"Cannot reduce stock by {quantity}. Only {StockQuantity} items available.");
        }

        StockQuantity -= quantity;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetCategory(SiteCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (category.Scope is not (CategoryScope.General or CategoryScope.Product))
        {
            throw new InvalidOperationException("Product categories must use product or general scope.");
        }

        Category = category;
        CategoryId = category.Id;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Publish(DateTimeOffset? publishedAt = null)
    {
        IsPublished = true;
        PublishedAt = publishedAt ?? DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Unpublish()
    {
        IsPublished = false;
        PublishedAt = null;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetFeaturedImage(string? imagePath)
    {
        FeaturedImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void AssignSeller(string? sellerId)
    {
        SellerId = string.IsNullOrWhiteSpace(sellerId) ? null : sellerId.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetBrand(string? brand)
    {
        Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetDigitalDownload(string? downloadPath)
    {
        if (Type == ProductType.Digital)
        {
            if (string.IsNullOrWhiteSpace(downloadPath))
            {
                throw new InvalidOperationException("Digital products require a download path.");
            }

            DigitalDownloadPath = downloadPath.Trim();
        }
        else
        {
            DigitalDownloadPath = null;
        }

        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetTags(IEnumerable<string>? tags)
    {
        if (tags is null)
        {
            TagList = string.Empty;
            UpdateDate = DateTimeOffset.UtcNow;
            return;
        }

        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            var normalized = tag.Trim();
            if (normalized.Length > MaxTagLength)
            {
                normalized = normalized[..MaxTagLength];
            }

            if (unique.Add(normalized))
            {
                ordered.Add(normalized);
            }
        }

        TagList = ordered.Count == 0 ? string.Empty : string.Join(',', ordered);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void ReplaceGallery(IEnumerable<(string Path, int Order)>? images)
    {
        _gallery.Clear();
        ApplyGallery(images);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void AddGalleryImage(string path, int order)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Image path cannot be empty", nameof(path));
        }

        _gallery.Add(new ProductImage(Id, path.Trim(), order));
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void RemoveGalleryImage(Guid imageId)
    {
        var image = _gallery.FirstOrDefault(item => item.Id == imageId);
        if (image is not null)
        {
            _gallery.Remove(image);
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }

    public ProductExecutionStep AddExecutionStep(string title, string? description, string? duration, int displayOrder)
    {
        var step = new ProductExecutionStep(Id, title, description, duration, displayOrder);
        _executionSteps.Add(step);
        UpdateDate = DateTimeOffset.UtcNow;
        return step;
    }

    public bool RemoveExecutionStep(Guid stepId)
    {
        var step = _executionSteps.FirstOrDefault(item => item.Id == stepId);
        if (step is null)
        {
            return false;
        }

        _executionSteps.Remove(step);
        UpdateDate = DateTimeOffset.UtcNow;
        return true;
    }

    public ProductFaq AddFaq(string question, string answer, int displayOrder)
    {
        var faq = new ProductFaq(Id, question, answer, displayOrder);
        _faqs.Add(faq);
        UpdateDate = DateTimeOffset.UtcNow;
        return faq;
    }

    public bool RemoveFaq(Guid faqId)
    {
        var faq = _faqs.FirstOrDefault(item => item.Id == faqId);
        if (faq is null)
        {
            return false;
        }

        _faqs.Remove(faq);
        UpdateDate = DateTimeOffset.UtcNow;
        return true;
    }

    public ProductAttribute AddAttribute(string key, string value, int displayOrder = 0)
    {
        var attribute = new ProductAttribute(Id, key, value, displayOrder);
        _attributes.Add(attribute);
        UpdateDate = DateTimeOffset.UtcNow;
        return attribute;
    }

    public bool RemoveAttribute(Guid attributeId)
    {
        var attribute = _attributes.FirstOrDefault(item => item.Id == attributeId);
        if (attribute is null)
        {
            return false;
        }

        _attributes.Remove(attribute);
        UpdateDate = DateTimeOffset.UtcNow;
        return true;
    }

    public void ApplyAttributes(IEnumerable<(string Key, string Value, int DisplayOrder)>? attributes)
    {
        if (attributes is null)
        {
            return;
        }

        foreach (var (key, value, displayOrder) in attributes)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            _attributes.Add(new ProductAttribute(Id, key.Trim(), value.Trim(), displayOrder));
        }
    }

    public void ReplaceAttributes(IEnumerable<(string Key, string Value, int DisplayOrder)>? attributes)
    {
        _attributes.Clear();
        ApplyAttributes(attributes);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateSeoMetadata(string seoTitle, string seoDescription, string seoKeywords, string seoSlug, string? robots)
        => UpdateSeo(seoTitle, seoDescription, seoKeywords, seoSlug, robots);

    // Variant Attribute Management
    public ProductVariantAttribute AddVariantAttribute(string name, IEnumerable<string>? options = null, int displayOrder = 0)
    {
        var attribute = new ProductVariantAttribute(Id, name, options, displayOrder);
        _variantAttributes.Add(attribute);
        UpdateDate = DateTimeOffset.UtcNow;
        return attribute;
    }

    public bool RemoveVariantAttribute(Guid attributeId)
    {
        var attribute = _variantAttributes.FirstOrDefault(a => a.Id == attributeId);
        if (attribute is null)
        {
            return false;
        }

        _variantAttributes.Remove(attribute);
        UpdateDate = DateTimeOffset.UtcNow;
        return true;
    }

    // Variant Management
    public ProductVariant AddVariant(
        decimal? price = null,
        decimal? compareAtPrice = null,
        int stockQuantity = 0,
        string? sku = null,
        string? imagePath = null,
        bool isActive = true)
    {
        var variant = new ProductVariant(Id, price, compareAtPrice, stockQuantity, sku, imagePath, isActive);
        _variants.Add(variant);
        UpdateDate = DateTimeOffset.UtcNow;
        return variant;
    }

    public bool RemoveVariant(Guid variantId)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId);
        if (variant is null)
        {
            return false;
        }

        _variants.Remove(variant);
        UpdateDate = DateTimeOffset.UtcNow;
        return true;
    }

    public ProductVariant? GetVariantById(Guid variantId)
    {
        return _variants.FirstOrDefault(v => v.Id == variantId);
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

            _gallery.Add(new ProductImage(Id, path.Trim(), order));
        }
    }

    private static IReadOnlyCollection<string> ParseTags(string? tagList)
    {
        if (string.IsNullOrWhiteSpace(tagList))
        {
            return Array.Empty<string>();
        }

        return tagList
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
