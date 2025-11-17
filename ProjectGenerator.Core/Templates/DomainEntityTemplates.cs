namespace ProjectGenerator.Templates;

public partial class TemplateProvider
{
    // ==================== Domain Entities ====================
    
    public string GetProductEntityTemplate()
    {
        return $@"using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using {_namespace}.Domain.Entities;
using {_namespace}.Domain.Enums;

namespace {_namespace}.Domain.Entities;

public sealed class Product : SeoEntity
{{
    private const int MaxTagLength = 50;
    
    private readonly List<ProductImage> _gallery = new();
    private readonly List<ProductExecutionStep> _executionSteps = new();
    private readonly List<ProductFaq> _faqs = new();
    private readonly List<ProductComment> _comments = new();

    public string Name {{ get; private set; }}
    public string Summary {{ get; private set; }}
    public string Description {{ get; private set; }}
    public ProductType Type {{ get; private set; }}
    public decimal Price {{ get; private set; }}
    public decimal? CompareAtPrice {{ get; private set; }}
    public bool TrackInventory {{ get; private set; }}
    public int StockQuantity {{ get; private set; }}
    public bool IsPublished {{ get; private set; }}
    public DateTimeOffset? PublishedAt {{ get; private set; }}
    public Guid CategoryId {{ get; private set; }}
    public SiteCategory Category {{ get; private set; }} = null!;
    public string? FeaturedImagePath {{ get; private set; }}
    public string TagList {{ get; private set; }}
    public string? DigitalDownloadPath {{ get; private set; }}
    public string? SellerId {{ get; private set; }}

    public IReadOnlyCollection<ProductImage> Gallery => _gallery.AsReadOnly();
    public IReadOnlyCollection<string> Tags => ParseTags(TagList);
    public IReadOnlyCollection<ProductExecutionStep> ExecutionSteps => _executionSteps.AsReadOnly();
    public IReadOnlyCollection<ProductFaq> Faqs => _faqs.AsReadOnly();
    public IReadOnlyCollection<ProductComment> Comments => _comments.AsReadOnly();

    [SetsRequiredMembers]
    private Product()
    {{
        Name = string.Empty;
        Summary = string.Empty;
        Description = string.Empty;
        TagList = string.Empty;
    }}

    [SetsRequiredMembers]
    public Product(
        string name,
        string summary,
        string description,
        ProductType type,
        decimal price,
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
        string? sellerId = null)
    {{
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
        
        if (isPublished)
        {{
            Publish(publishedAt);
        }}
        else
        {{
            Unpublish();
        }}
    }}

    public void UpdateContent(string name, string summary, string description)
    {{
        if (string.IsNullOrWhiteSpace(name))
        {{
            throw new ArgumentException(""Product name cannot be empty"", nameof(name));
        }}

        Name = name.Trim();
        Summary = (summary ?? string.Empty).Trim();
        Description = (description ?? string.Empty).Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void ChangeType(ProductType type, string? digitalDownloadPath = null)
    {{
        Type = type;
        if (type == ProductType.Digital)
        {{
            SetDigitalDownload(digitalDownloadPath);
            TrackInventory = false;
            StockQuantity = 0;
        }}
        else
        {{
            DigitalDownloadPath = null;
        }}
        
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void UpdatePricing(decimal price, decimal? compareAtPrice)
    {{
        if (price < 0)
        {{
            throw new ArgumentOutOfRangeException(nameof(price));
        }}
        
        if (compareAtPrice is < 0)
        {{
            throw new ArgumentOutOfRangeException(nameof(compareAtPrice));
        }}
        
        Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        CompareAtPrice = compareAtPrice is null 
            ? null 
            : decimal.Round(compareAtPrice.Value, 2, MidpointRounding.AwayFromZero);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void UpdateInventory(bool trackInventory, int stockQuantity)
    {{
        if (trackInventory && Type == ProductType.Digital)
        {{
            trackInventory = false;
            stockQuantity = 0;
        }}
        
        if (stockQuantity < 0)
        {{
            throw new ArgumentOutOfRangeException(nameof(stockQuantity));
        }}
        
        TrackInventory = trackInventory;
        StockQuantity = trackInventory ? stockQuantity : 0;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetCategory(SiteCategory category)
    {{
        ArgumentNullException.ThrowIfNull(category);
        Category = category;
        CategoryId = category.Id;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void Publish(DateTimeOffset? publishedAt = null)
    {{
        IsPublished = true;
        PublishedAt = publishedAt ?? DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void Unpublish()
    {{
        IsPublished = false;
        PublishedAt = null;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetFeaturedImage(string? imagePath)
    {{
        FeaturedImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void AssignSeller(string? sellerId)
    {{
        SellerId = string.IsNullOrWhiteSpace(sellerId) ? null : sellerId.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetDigitalDownload(string? downloadPath)
    {{
        if (Type == ProductType.Digital)
        {{
            if (string.IsNullOrWhiteSpace(downloadPath))
            {{
                throw new InvalidOperationException(""Digital products require a download path."");
            }}
            
            DigitalDownloadPath = downloadPath.Trim();
        }}
        else
        {{
            DigitalDownloadPath = null;
        }}
        
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetTags(IEnumerable<string>? tags)
    {{
        if (tags is null)
        {{
            TagList = string.Empty;
            UpdateDate = DateTimeOffset.UtcNow;
            return;
        }}
        
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        foreach (var tag in tags)
        {{
            if (string.IsNullOrWhiteSpace(tag))
            {{
                continue;
            }}
            
            var normalized = tag.Trim();
            if (normalized.Length > MaxTagLength)
            {{
                normalized = normalized[..MaxTagLength];
            }}
            
            if (unique.Add(normalized))
            {{
                ordered.Add(normalized);
            }}
        }}
        
        TagList = ordered.Count == 0 ? string.Empty : string.Join(',', ordered);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void ReplaceGallery(IEnumerable<(string Path, int Order)>? images)
    {{
        _gallery.Clear();
        ApplyGallery(images);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public ProductComment AddComment(
        string authorName,
        string content,
        double rating,
        Guid? parentId = null,
        bool isApproved = false)
    {{
        ProductComment? parent = null;
        if (parentId.HasValue)
        {{
            parent = _comments.FirstOrDefault(comment => comment.Id == parentId.Value);
            if (parent is null)
            {{
                throw new InvalidOperationException(""Parent comment could not be found."");
            }}
        }}
        
        var comment = new ProductComment(Id, authorName, content, rating, parent, isApproved);
        _comments.Add(comment);
        UpdateDate = DateTimeOffset.UtcNow;
        return comment;
    }}

    public ProductExecutionStep AddExecutionStep(string title, string? description, string? duration, int displayOrder)
    {{
        var step = new ProductExecutionStep(Id, title, description, duration, displayOrder);
        _executionSteps.Add(step);
        UpdateDate = DateTimeOffset.UtcNow;
        return step;
    }}

    public ProductFaq AddFaq(string question, string answer, int displayOrder)
    {{
        var faq = new ProductFaq(Id, question, answer, displayOrder);
        _faqs.Add(faq);
        UpdateDate = DateTimeOffset.UtcNow;
        return faq;
    }}

    private void ApplyGallery(IEnumerable<(string Path, int Order)>? images)
    {{
        if (images is null)
        {{
            return;
        }}
        
        foreach (var (path, order) in images)
        {{
            if (string.IsNullOrWhiteSpace(path))
            {{
                continue;
            }}
            
            _gallery.Add(new ProductImage(Id, path.Trim(), order));
        }}
    }}

    private static IReadOnlyCollection<string> ParseTags(string? tagList)
    {{
        if (string.IsNullOrWhiteSpace(tagList))
        {{
            return Array.Empty<string>();
        }}
        
        return tagList
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }}
}}
";
    }

    public string GetSeoEntityTemplate()
    {
        return $@"using System;

namespace {_namespace}.Domain.Base;

public abstract class SeoEntity : Entity
{{
    public string SeoTitle {{ get; protected set; }} = string.Empty;
    public string SeoDescription {{ get; protected set; }} = string.Empty;
    public string SeoKeywords {{ get; protected set; }} = string.Empty;
    public string SeoSlug {{ get; protected set; }} = string.Empty;
    public string? Robots {{ get; protected set; }}

    protected void UpdateSeo(string seoTitle, string seoDescription, string seoKeywords, string seoSlug, string? robots)
    {{
        SeoTitle = seoTitle?.Trim() ?? string.Empty;
        SeoDescription = seoDescription?.Trim() ?? string.Empty;
        SeoKeywords = seoKeywords?.Trim() ?? string.Empty;
        SeoSlug = seoSlug?.Trim() ?? string.Empty;
        Robots = string.IsNullOrWhiteSpace(robots) ? null : robots.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    public string GetEnhancedApplicationUserTemplate()
    {
        return $@"using Microsoft.AspNetCore.Identity;

namespace {_namespace}.Domain.Entities;

public sealed class ApplicationUser : IdentityUser
{{
    public string FullName {{ get; set; }} = string.Empty;
    public bool IsActive {{ get; set; }} = true;
    public DateTimeOffset? DeactivatedOn {{ get; set; }}
    public string? DeactivationReason {{ get; set; }}
    public bool IsDeleted {{ get; set; }}
    public DateTimeOffset? DeletedOn {{ get; set; }}
    public DateTimeOffset CreatedOn {{ get; set; }} = DateTimeOffset.UtcNow;
    public DateTimeOffset LastModifiedOn {{ get; set; }} = DateTimeOffset.UtcNow;
    public string? AvatarPath {{ get; set; }}
}}
";
    }

    public string GetAccessPermissionEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class AccessPermission : Entity
{{
    private DateTimeOffset? _updatedAt;

    public string Key {{ get; private set; }} = string.Empty;
    public string DisplayName {{ get; private set; }} = string.Empty;
    public string? Description {{ get; private set; }}
    public bool IsCore {{ get; private set; }}
    public string GroupKey {{ get; private set; }} = ""custom"";
    public string GroupDisplayName {{ get; private set; }} = string.Empty;

    public DateTimeOffset CreatedAt
    {{
        get => CreateDate;
        private set => CreateDate = value;
    }}

    public DateTimeOffset? UpdatedAt
    {{
        get => _updatedAt;
        private set
        {{
            _updatedAt = value;
            if (value.HasValue)
            {{
                UpdateDate = value.Value;
            }}
        }}
    }}

    [SetsRequiredMembers]
    private AccessPermission()
    {{
    }}

    [SetsRequiredMembers]
    public AccessPermission(
        string key,
        string displayName,
        string? description,
        bool isCore,
        string groupKey,
        string groupDisplayName)
    {{
        Key = key;
        DisplayName = displayName;
        Description = description;
        IsCore = isCore;
        GroupKey = string.IsNullOrWhiteSpace(groupKey) ? ""custom"" : groupKey;
        GroupDisplayName = string.IsNullOrWhiteSpace(groupDisplayName) 
            ? GroupKey 
            : groupDisplayName;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = null;
        Ip = System.Net.IPAddress.None;
    }}

    public void UpdateDetails(
        string displayName,
        string? description,
        bool isCore,
        string groupKey,
        string groupDisplayName)
    {{
        DisplayName = displayName;
        Description = description;
        IsCore = isCore;
        if (!string.IsNullOrWhiteSpace(groupKey))
        {{
            GroupKey = groupKey;
        }}
        if (!string.IsNullOrWhiteSpace(groupDisplayName))
        {{
            GroupDisplayName = groupDisplayName;
        }}
        UpdatedAt = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    public string GetPageAccessPolicyEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class PageAccessPolicy : Entity
{{
    public string Area {{ get; private set; }} = string.Empty;
    public string Controller {{ get; private set; }} = string.Empty;
    public string Action {{ get; private set; }} = string.Empty;
    public string PermissionKey {{ get; private set; }} = string.Empty;

    [SetsRequiredMembers]
    private PageAccessPolicy()
    {{
    }}

    [SetsRequiredMembers]
    public PageAccessPolicy(string area, string controller, string action, string permissionKey)
    {{
        SetRoute(area, controller, action);
        UpdatePermission(permissionKey);
    }}

    public void SetRoute(string area, string controller, string action)
    {{
        ArgumentException.ThrowIfNullOrWhiteSpace(controller);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        
        Area = string.IsNullOrWhiteSpace(area) ? string.Empty : area.Trim();
        Controller = controller.Trim();
        Action = action.Trim();
    }}

    public void UpdatePermission(string permissionKey)
    {{
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionKey);
        PermissionKey = permissionKey.Trim();
    }}
}}
";
    }

    public string GetUserSessionEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class UserSession : Entity
{{
    public string UserId {{ get; private set; }} = string.Empty;
    public IPAddress? ClientIp {{ get; private set; }}
    public string DeviceType {{ get; private set; }} = string.Empty;
    public string ClientName {{ get; private set; }} = string.Empty;
    public string UserAgent {{ get; private set; }} = string.Empty;
    public DateTimeOffset StartedAt {{ get; private set; }}
    public DateTimeOffset? EndedAt {{ get; private set; }}

    [SetsRequiredMembers]
    private UserSession()
    {{
    }}

    [SetsRequiredMembers]
    public static UserSession Start(
        string userId,
        IPAddress? clientIp,
        string deviceType,
        string clientName,
        string userAgent)
    {{
        return new UserSession
        {{
            UserId = userId,
            ClientIp = clientIp,
            DeviceType = deviceType ?? ""Unknown"",
            ClientName = clientName ?? ""Unknown"",
            UserAgent = userAgent ?? string.Empty,
            StartedAt = DateTimeOffset.UtcNow,
            Ip = clientIp ?? IPAddress.None
        }};
    }}

    public void End()
    {{
        EndedAt = DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    public string GetSellerProfileEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Domain.Entities;

public sealed class SellerProfile : Entity
{{
    public string DisplayName {{ get; private set; }}
    public string? Degree {{ get; private set; }}
    public string? Specialty {{ get; private set; }}
    public string? Bio {{ get; private set; }}
    public string? AvatarUrl {{ get; private set; }}
    public string? ContactEmail {{ get; private set; }}
    public string? ContactPhone {{ get; private set; }}
    public string? UserId {{ get; private set; }}
    public bool IsActive {{ get; private set; }}
    public ApplicationUser? User {{ get; private set; }}

    [SetsRequiredMembers]
    private SellerProfile()
    {{
        DisplayName = string.Empty;
    }}

    [SetsRequiredMembers]
    public SellerProfile(
        string displayName,
        string? degree,
        string? specialty,
        string? bio,
        string? avatarUrl,
        string? contactEmail,
        string? contactPhone,
        string? userId,
        bool isActive = true)
    {{
        UpdateDisplayName(displayName);
        UpdateAcademicInfo(degree, specialty, bio);
        UpdateMedia(avatarUrl);
        UpdateContact(contactEmail, contactPhone);
        ConnectToUser(userId);
        IsActive = isActive;
    }}

    public void UpdateDisplayName(string displayName)
    {{
        if (string.IsNullOrWhiteSpace(displayName))
        {{
            throw new ArgumentException(""نام فروشنده الزامی است."", nameof(displayName));
        }}
        
        DisplayName = displayName.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void UpdateAcademicInfo(string? degree, string? specialty, string? bio)
    {{
        Degree = NormalizeOptional(degree);
        Specialty = NormalizeOptional(specialty);
        Bio = NormalizeOptional(bio);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void UpdateMedia(string? avatarUrl)
    {{
        AvatarUrl = NormalizeOptional(avatarUrl);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void UpdateContact(string? email, string? phone)
    {{
        ContactEmail = NormalizeOptional(email);
        ContactPhone = NormalizeOptional(phone);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void ConnectToUser(string? userId)
    {{
        UserId = NormalizeOptional(userId);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetActive(bool isActive)
    {{
        IsActive = isActive;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    private static string? NormalizeOptional(string? value)
    {{
        if (string.IsNullOrWhiteSpace(value))
        {{
            return null;
        }}
        
        return value.Trim();
    }}
}}
";
    }

    public string GetBlogEntityTemplate()
    {
        return $@"using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using {_namespace}.Domain.Base;
using {_namespace}.Domain.Enums;

namespace {_namespace}.Domain.Entities;

public sealed class Blog : SeoEntity
{{
    private const int MaxTagLength = 50;
    
    private readonly List<BlogComment> _comments = new();

    public string Title {{ get; private set; }}
    public string Summary {{ get; private set; }}
    public string Content {{ get; private set; }}
    public int ReadingTimeMinutes {{ get; private set; }}
    public BlogStatus Status {{ get; private set; }}
    public DateTimeOffset? PublishedAt {{ get; private set; }}
    public Guid CategoryId {{ get; private set; }}
    public BlogCategory Category {{ get; private set; }} = null!;
    public Guid AuthorId {{ get; private set; }}
    public BlogAuthor Author {{ get; private set; }} = null!;
    public int LikeCount {{ get; private set; }}
    public int DislikeCount {{ get; private set; }}
    public string? FeaturedImagePath {{ get; private set; }}
    public string TagList {{ get; private set; }}

    public IReadOnlyCollection<BlogComment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<string> Tags => ParseTags(TagList);

    [SetsRequiredMembers]
    private Blog()
    {{
        Title = string.Empty;
        Summary = string.Empty;
        Content = string.Empty;
        Status = BlogStatus.Draft;
        TagList = string.Empty;
    }}

    [SetsRequiredMembers]
    public Blog(
        string title,
        string summary,
        string content,
        BlogCategory category,
        BlogAuthor author,
        BlogStatus status,
        int readingTimeMinutes,
        string seoTitle,
        string seoDescription,
        string seoKeywords,
        string seoSlug,
        string? robots,
        string? featuredImagePath,
        IEnumerable<string>? tags,
        DateTimeOffset? publishedAt = null)
    {{
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(author);
        
        UpdateContent(title, summary, content, readingTimeMinutes);
        SetCategory(category);
        SetAuthor(author);
        ChangeStatus(status, publishedAt);
        UpdateSeo(seoTitle, seoDescription, seoKeywords, seoSlug, robots);
        SetFeaturedImage(featuredImagePath);
        SetTags(tags);
    }}

    public void UpdateContent(string title, string summary, string content, int readingTimeMinutes)
    {{
        if (string.IsNullOrWhiteSpace(title))
        {{
            throw new ArgumentException(""Blog title cannot be empty"", nameof(title));
        }}
        
        Title = title.Trim();
        Summary = (summary ?? string.Empty).Trim();
        Content = content.Trim();
        ReadingTimeMinutes = Math.Max(1, readingTimeMinutes);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetCategory(BlogCategory category)
    {{
        ArgumentNullException.ThrowIfNull(category);
        Category = category;
        CategoryId = category.Id;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetAuthor(BlogAuthor author)
    {{
        ArgumentNullException.ThrowIfNull(author);
        Author = author;
        AuthorId = author.Id;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void ChangeStatus(BlogStatus status, DateTimeOffset? publishedAt = null)
    {{
        Status = status;
        if (status == BlogStatus.Published)
        {{
            PublishedAt = publishedAt ?? DateTimeOffset.UtcNow;
        }}
        
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetFeaturedImage(string? imagePath)
    {{
        FeaturedImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetTags(IEnumerable<string>? tags)
    {{
        if (tags is null)
        {{
            TagList = string.Empty;
            UpdateDate = DateTimeOffset.UtcNow;
            return;
        }}
        
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        foreach (var tag in tags)
        {{
            if (string.IsNullOrWhiteSpace(tag))
            {{
                continue;
            }}
            
            var normalized = tag.Trim();
            if (normalized.Length > MaxTagLength)
            {{
                normalized = normalized[..MaxTagLength];
            }}
            
            if (unique.Add(normalized))
            {{
                ordered.Add(normalized);
            }}
        }}
        
        TagList = ordered.Count == 0 ? string.Empty : string.Join(',', ordered);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    private static IReadOnlyCollection<string> ParseTags(string tagList)
    {{
        if (string.IsNullOrWhiteSpace(tagList))
        {{
            return Array.Empty<string>();
        }}
        
        return tagList
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .ToArray();
    }}
}}
";
    }

    public string GetSiteCategoryEntityTemplate()
    {
        return $@"using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;
using {_namespace}.Domain.Enums;

namespace {_namespace}.Domain.Entities;

public sealed class SiteCategory : Entity
{{
    private readonly List<SiteCategory> _children = new();

    public string Name {{ get; private set; }} = string.Empty;
    public string? Slug {{ get; private set; }}
    public string? Description {{ get; private set; }}
    public CategoryScope Scope {{ get; private set; }}
    public Guid? ParentId {{ get; private set; }}
    public SiteCategory? Parent {{ get; private set; }}
    public int Depth {{ get; private set; }}

    public IReadOnlyCollection<SiteCategory> Children => _children.AsReadOnly();

    [SetsRequiredMembers]
    private SiteCategory()
    {{
    }}

    [SetsRequiredMembers]
    public SiteCategory(
        string name,
        string? slug,
        string? description,
        CategoryScope scope,
        SiteCategory? parent = null)
    {{
        SetName(name);
        SetSlug(slug);
        SetDescription(description);
        Scope = scope;
        SetParent(parent);
    }}

    public void SetName(string name)
    {{
        if (string.IsNullOrWhiteSpace(name))
        {{
            throw new ArgumentException(""Category name cannot be empty"", nameof(name));
        }}
        
        Name = name.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetSlug(string? slug)
    {{
        Slug = string.IsNullOrWhiteSpace(slug) ? null : slug.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetDescription(string? description)
    {{
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetParent(SiteCategory? parent)
    {{
        Parent = parent;
        ParentId = parent?.Id;
        Depth = parent is null ? 0 : parent.Depth + 1;
        UpdateDate = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    public string GetNavigationMenuItemEntityTemplate()
    {
        return $@"using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class NavigationMenuItem : Entity
{{
    private readonly List<NavigationMenuItem> _children = new();

    public string Title {{ get; private set; }} = string.Empty;
    public string? Url {{ get; private set; }}
    public int DisplayOrder {{ get; private set; }}
    public Guid? ParentId {{ get; private set; }}
    public NavigationMenuItem? Parent {{ get; private set; }}
    public bool IsExternal {{ get; private set; }}
    public bool OpenInNewTab {{ get; private set; }}

    public IReadOnlyCollection<NavigationMenuItem> Children => _children.AsReadOnly();

    [SetsRequiredMembers]
    private NavigationMenuItem()
    {{
    }}

    [SetsRequiredMembers]
    public NavigationMenuItem(
        string title,
        string? url,
        int displayOrder,
        NavigationMenuItem? parent = null,
        bool isExternal = false,
        bool openInNewTab = false)
    {{
        SetTitle(title);
        SetUrl(url);
        SetDisplayOrder(displayOrder);
        SetParent(parent);
        IsExternal = isExternal;
        OpenInNewTab = openInNewTab;
    }}

    public void SetTitle(string title)
    {{
        if (string.IsNullOrWhiteSpace(title))
        {{
            throw new ArgumentException(""Menu item title cannot be empty"", nameof(title));
        }}
        
        Title = title.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetUrl(string? url)
    {{
        Url = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetDisplayOrder(int displayOrder)
    {{
        DisplayOrder = displayOrder;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetParent(NavigationMenuItem? parent)
    {{
        Parent = parent;
        ParentId = parent?.Id;
        UpdateDate = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    public string GetSiteSettingEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class SiteSetting : Entity
{{
    public string SiteName {{ get; private set; }} = string.Empty;
    public string? SiteDescription {{ get; private set; }}
    public string? ContactEmail {{ get; private set; }}
    public string? ContactPhone {{ get; private set; }}
    public string? Address {{ get; private set; }}
    public string? LogoPath {{ get; private set; }}
    public string? FaviconPath {{ get; private set; }}

    [SetsRequiredMembers]
    private SiteSetting()
    {{
    }}

    [SetsRequiredMembers]
    public SiteSetting(
        string siteName,
        string? siteDescription,
        string? contactEmail,
        string? contactPhone,
        string? address,
        string? logoPath,
        string? faviconPath)
    {{
        SetSiteName(siteName);
        SetSiteDescription(siteDescription);
        SetContactEmail(contactEmail);
        SetContactPhone(contactPhone);
        SetAddress(address);
        SetLogoPath(logoPath);
        SetFaviconPath(faviconPath);
    }}

    public void SetSiteName(string siteName)
    {{
        if (string.IsNullOrWhiteSpace(siteName))
        {{
            throw new ArgumentException(""Site name cannot be empty"", nameof(siteName));
        }}
        
        SiteName = siteName.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetSiteDescription(string? description)
    {{
        SiteDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetContactEmail(string? email)
    {{
        ContactEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetContactPhone(string? phone)
    {{
        ContactPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetAddress(string? address)
    {{
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetLogoPath(string? logoPath)
    {{
        LogoPath = string.IsNullOrWhiteSpace(logoPath) ? null : logoPath.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetFaviconPath(string? faviconPath)
    {{
        FaviconPath = string.IsNullOrWhiteSpace(faviconPath) ? null : faviconPath.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    public string GetBlogAuthorEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class BlogAuthor : Entity
{{
    public string DisplayName {{ get; private set; }} = string.Empty;
    public string? Bio {{ get; private set; }}
    public string? AvatarUrl {{ get; private set; }}
    public bool IsActive {{ get; private set; }}
    public string? UserId {{ get; private set; }}

    [SetsRequiredMembers]
    private BlogAuthor()
    {{
    }}

    [SetsRequiredMembers]
    public BlogAuthor(string displayName, string? bio, string? avatarUrl, bool isActive, string? userId)
    {{
        SetDisplayName(displayName);
        SetBio(bio);
        SetAvatarUrl(avatarUrl);
        IsActive = isActive;
        SetUserId(userId);
    }}

    public void SetDisplayName(string displayName)
    {{
        if (string.IsNullOrWhiteSpace(displayName))
        {{
            throw new ArgumentException(""Author display name cannot be empty"", nameof(displayName));
        }}
        
        DisplayName = displayName.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetBio(string? bio)
    {{
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetAvatarUrl(string? avatarUrl)
    {{
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetUserId(string? userId)
    {{
        UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void Activate()
    {{
        IsActive = true;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void Deactivate()
    {{
        IsActive = false;
        UpdateDate = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    public string GetBlogCategoryEntityTemplate()
    {
        return $@"using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class BlogCategory : Entity
{{
    private readonly List<BlogCategory> _children = new();

    public string Name {{ get; private set; }} = string.Empty;
    public string? Slug {{ get; private set; }}
    public string? Description {{ get; private set; }}
    public Guid? ParentId {{ get; private set; }}
    public BlogCategory? Parent {{ get; private set; }}
    public int Depth {{ get; private set; }}

    public IReadOnlyCollection<BlogCategory> Children => _children.AsReadOnly();

    [SetsRequiredMembers]
    private BlogCategory()
    {{
    }}

    [SetsRequiredMembers]
    public BlogCategory(string name, string? slug, string? description, BlogCategory? parent = null)
    {{
        SetName(name);
        SetSlug(slug);
        SetDescription(description);
        SetParent(parent);
    }}

    public void SetName(string name)
    {{
        if (string.IsNullOrWhiteSpace(name))
        {{
            throw new ArgumentException(""Category name cannot be empty"", nameof(name));
        }}
        
        Name = name.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetSlug(string? slug)
    {{
        Slug = string.IsNullOrWhiteSpace(slug) ? null : slug.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetDescription(string? description)
    {{
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetParent(BlogCategory? parent)
    {{
        Parent = parent;
        ParentId = parent?.Id;
        Depth = parent is null ? 0 : parent.Depth + 1;
        UpdateDate = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    public string GetInvoiceEntityTemplate()
    {
        return $@"using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using {_namespace}.Domain.Base;
using {_namespace}.Domain.Enums;

namespace {_namespace}.Domain.Entities;

public sealed class Invoice : Entity
{{
    private readonly List<InvoiceItem> _items = new();
    private readonly List<Transaction> _transactions = new();

    public string InvoiceNumber {{ get; private set; }}
    public string Title {{ get; private set; }}
    public string? Description {{ get; private set; }}
    public string UserId {{ get; private set; }}
    public Currency Currency {{ get; private set; }}
    public InvoiceStatus Status {{ get; private set; }}
    public DateTimeOffset IssueDate {{ get; private set; }}
    public DateTimeOffset? DueDate {{ get; private set; }}
    public decimal TaxAmount {{ get; private set; }}
    public decimal AdjustmentAmount {{ get; private set; }}
    public string? ExternalReference {{ get; private set; }}

    public ApplicationUser User {{ get; private set; }} = null!;

    public IReadOnlyCollection<InvoiceItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    public decimal SubTotal => _items.Sum(i => i.Total);
    public decimal Total => SubTotal + TaxAmount + AdjustmentAmount;
    public decimal TotalPaid => _transactions.Where(t => t.IsSuccessful).Sum(t => t.Amount);
    public decimal Balance => Total - TotalPaid;

    [SetsRequiredMembers]
    private Invoice()
    {{
        InvoiceNumber = string.Empty;
        Title = string.Empty;
        UserId = string.Empty;
        Currency = Currency.IRR;
        Status = InvoiceStatus.Draft;
        IssueDate = DateTimeOffset.UtcNow;
    }}

    [SetsRequiredMembers]
    public Invoice(
        string invoiceNumber,
        string title,
        string? description,
        string userId,
        Currency currency,
        DateTimeOffset issueDate,
        DateTimeOffset? dueDate,
        decimal taxAmount,
        decimal adjustmentAmount,
        IEnumerable<(string Description, decimal UnitPrice, int Quantity)>? items = null)
    {{
        SetInvoiceNumber(invoiceNumber);
        SetTitle(title);
        SetDescription(description);
        SetUserId(userId);
        Currency = currency;
        IssueDate = issueDate;
        DueDate = dueDate;
        SetTaxAmount(taxAmount);
        SetAdjustmentAmount(adjustmentAmount);
        Status = InvoiceStatus.Issued;

        if (items != null)
        {{
            foreach (var (description, unitPrice, quantity) in items)
            {{
                AddItem(description, unitPrice, quantity);
            }}
        }}
    }}

    public void SetInvoiceNumber(string invoiceNumber)
    {{
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {{
            throw new ArgumentException(""Invoice number cannot be empty"", nameof(invoiceNumber));
        }}
        
        InvoiceNumber = invoiceNumber.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetTitle(string title)
    {{
        if (string.IsNullOrWhiteSpace(title))
        {{
            throw new ArgumentException(""Invoice title cannot be empty"", nameof(title));
        }}
        
        Title = title.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetDescription(string? description)
    {{
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetUserId(string userId)
    {{
        if (string.IsNullOrWhiteSpace(userId))
        {{
            throw new ArgumentException(""User ID cannot be empty"", nameof(userId));
        }}
        
        UserId = userId.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetTaxAmount(decimal taxAmount)
    {{
        if (taxAmount < 0)
        {{
            throw new ArgumentOutOfRangeException(nameof(taxAmount));
        }}
        
        TaxAmount = decimal.Round(taxAmount, 2, MidpointRounding.AwayFromZero);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetAdjustmentAmount(decimal adjustmentAmount)
    {{
        AdjustmentAmount = decimal.Round(adjustmentAmount, 2, MidpointRounding.AwayFromZero);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void AddItem(string description, decimal unitPrice, int quantity)
    {{
        var item = new InvoiceItem(Id, description, unitPrice, quantity);
        _items.Add(item);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void MarkPaid()
    {{
        Status = InvoiceStatus.Paid;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void Cancel()
    {{
        Status = InvoiceStatus.Cancelled;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void Reopen()
    {{
        Status = InvoiceStatus.Issued;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public Transaction RecordTransaction(
        decimal amount,
        string gateway,
        string? transactionId,
        bool isSuccessful,
        string? notes)
    {{
        var transaction = new Transaction(
            Id,
            UserId,
            amount,
            Currency,
            gateway,
            transactionId,
            isSuccessful,
            notes);
        
        _transactions.Add(transaction);
        
        if (Balance <= 0)
        {{
            MarkPaid();
        }}
        
        UpdateDate = DateTimeOffset.UtcNow;
        return transaction;
    }}
}}
";
    }

    public string GetDiscountCodeEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;
using {_namespace}.Domain.Enums;

namespace {_namespace}.Domain.Entities;

public sealed class DiscountCode : Entity
{{
    public string Code {{ get; private set; }}
    public DiscountType Type {{ get; private set; }}
    public decimal Value {{ get; private set; }}
    public int? MaxUsageCount {{ get; private set; }}
    public int UsedCount {{ get; private set; }}
    public DateTimeOffset? ValidFrom {{ get; private set; }}
    public DateTimeOffset? ValidUntil {{ get; private set; }}
    public bool IsActive {{ get; private set; }}
    public string? Description {{ get; private set; }}
    public decimal? MinimumPurchaseAmount {{ get; private set; }}
    public decimal? MaximumDiscountAmount {{ get; private set; }}

    [SetsRequiredMembers]
    private DiscountCode()
    {{
        Code = string.Empty;
        Type = DiscountType.Percentage;
    }}

    [SetsRequiredMembers]
    public DiscountCode(
        string code,
        DiscountType type,
        decimal value,
        int? maxUsageCount,
        DateTimeOffset? validFrom,
        DateTimeOffset? validUntil,
        bool isActive,
        string? description,
        decimal? minimumPurchaseAmount,
        decimal? maximumDiscountAmount)
    {{
        SetCode(code);
        SetDiscount(type, value);
        SetUsageLimit(maxUsageCount);
        SetValidity(validFrom, validUntil);
        IsActive = isActive;
        SetDescription(description);
        SetMinimumPurchase(minimumPurchaseAmount);
        SetMaximumDiscount(maximumDiscountAmount);
        UsedCount = 0;
    }}

    public void SetCode(string code)
    {{
        if (string.IsNullOrWhiteSpace(code))
        {{
            throw new ArgumentException(""Discount code cannot be empty"", nameof(code));
        }}
        
        Code = code.Trim().ToUpperInvariant();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetDiscount(DiscountType type, decimal value)
    {{
        if (value <= 0)
        {{
            throw new ArgumentOutOfRangeException(nameof(value), ""Discount value must be positive"");
        }}
        
        if (type == DiscountType.Percentage && value > 100)
        {{
            throw new ArgumentOutOfRangeException(nameof(value), ""Percentage discount cannot exceed 100%"");
        }}
        
        Type = type;
        Value = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetUsageLimit(int? maxUsageCount)
    {{
        if (maxUsageCount is < 1)
        {{
            throw new ArgumentOutOfRangeException(nameof(maxUsageCount));
        }}
        
        MaxUsageCount = maxUsageCount;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetValidity(DateTimeOffset? validFrom, DateTimeOffset? validUntil)
    {{
        if (validFrom.HasValue && validUntil.HasValue && validFrom.Value > validUntil.Value)
        {{
            throw new InvalidOperationException(""Valid from date cannot be after valid until date"");
        }}
        
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetDescription(string? description)
    {{
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetMinimumPurchase(decimal? minimumPurchaseAmount)
    {{
        if (minimumPurchaseAmount is < 0)
        {{
            throw new ArgumentOutOfRangeException(nameof(minimumPurchaseAmount));
        }}
        
        MinimumPurchaseAmount = minimumPurchaseAmount;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetMaximumDiscount(decimal? maximumDiscountAmount)
    {{
        if (maximumDiscountAmount is < 0)
        {{
            throw new ArgumentOutOfRangeException(nameof(maximumDiscountAmount));
        }}
        
        MaximumDiscountAmount = maximumDiscountAmount;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void Activate()
    {{
        IsActive = true;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void Deactivate()
    {{
        IsActive = false;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void IncrementUsage()
    {{
        UsedCount++;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public bool IsValid(DateTimeOffset? now = null)
    {{
        if (!IsActive)
        {{
            return false;
        }}
        
        if (MaxUsageCount.HasValue && UsedCount >= MaxUsageCount.Value)
        {{
            return false;
        }}
        
        var checkTime = now ?? DateTimeOffset.UtcNow;
        
        if (ValidFrom.HasValue && checkTime < ValidFrom.Value)
        {{
            return false;
        }}
        
        if (ValidUntil.HasValue && checkTime > ValidUntil.Value)
        {{
            return false;
        }}
        
        return true;
    }}

    public decimal CalculateDiscount(decimal purchaseAmount)
    {{
        if (!IsValid())
        {{
            return 0;
        }}
        
        if (MinimumPurchaseAmount.HasValue && purchaseAmount < MinimumPurchaseAmount.Value)
        {{
            return 0;
        }}
        
        var discount = Type == DiscountType.Percentage 
            ? purchaseAmount * (Value / 100m)
            : Value;
        
        if (MaximumDiscountAmount.HasValue && discount > MaximumDiscountAmount.Value)
        {{
            discount = MaximumDiscountAmount.Value;
        }}
        
        return decimal.Round(discount, 2, MidpointRounding.AwayFromZero);
    }}
}}
";
    }

    public string GetWalletAccountEntityTemplate()
    {
        return $@"using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class WalletAccount : Entity
{{
    private readonly List<WalletTransaction> _transactions = new();

    public string UserId {{ get; private set; }}
    public ApplicationUser User {{ get; private set; }} = null!;
    public decimal Balance {{ get; private set; }}

    public IReadOnlyCollection<WalletTransaction> Transactions => _transactions.AsReadOnly();

    [SetsRequiredMembers]
    private WalletAccount()
    {{
        UserId = string.Empty;
        Balance = 0;
    }}

    [SetsRequiredMembers]
    public WalletAccount(string userId)
    {{
        SetUserId(userId);
        Balance = 0;
    }}

    public void SetUserId(string userId)
    {{
        if (string.IsNullOrWhiteSpace(userId))
        {{
            throw new ArgumentException(""User ID cannot be empty"", nameof(userId));
        }}
        
        UserId = userId.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public WalletTransaction Credit(decimal amount, string description, string? referenceId = null)
    {{
        if (amount <= 0)
        {{
            throw new ArgumentOutOfRangeException(nameof(amount), ""Amount must be positive"");
        }}
        
        var transaction = new WalletTransaction(Id, amount, description, referenceId);
        _transactions.Add(transaction);
        Balance += amount;
        UpdateDate = DateTimeOffset.UtcNow;
        return transaction;
    }}

    public WalletTransaction Debit(decimal amount, string description, string? referenceId = null)
    {{
        if (amount <= 0)
        {{
            throw new ArgumentOutOfRangeException(nameof(amount), ""Amount must be positive"");
        }}
        
        if (Balance < amount)
        {{
            throw new InvalidOperationException(""Insufficient balance"");
        }}
        
        var transaction = new WalletTransaction(Id, -amount, description, referenceId);
        _transactions.Add(transaction);
        Balance -= amount;
        UpdateDate = DateTimeOffset.UtcNow;
        return transaction;
    }}

    public bool CanDebit(decimal amount)
    {{
        return amount > 0 && Balance >= amount;
    }}
}}
";
    }

    public string GetFinancialSettingsEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class FinancialSettings : Entity
{{
    public decimal SellerCommissionPercentage {{ get; private set; }}
    public decimal TaxPercentage {{ get; private set; }}
    public decimal SalesFeePercentage {{ get; private set; }}
    public decimal MinimumWithdrawalAmount {{ get; private set; }}

    [SetsRequiredMembers]
    private FinancialSettings()
    {{
    }}

    [SetsRequiredMembers]
    public FinancialSettings(
        decimal sellerCommissionPercentage,
        decimal taxPercentage,
        decimal salesFeePercentage,
        decimal minimumWithdrawalAmount)
    {{
        SetSellerCommission(sellerCommissionPercentage);
        SetTax(taxPercentage);
        SetSalesFee(salesFeePercentage);
        SetMinimumWithdrawal(minimumWithdrawalAmount);
    }}

    public void SetSellerCommission(decimal percentage)
    {{
        if (percentage < 0 || percentage > 100)
        {{
            throw new ArgumentOutOfRangeException(nameof(percentage), ""Percentage must be between 0 and 100"");
        }}
        
        SellerCommissionPercentage = decimal.Round(percentage, 2, MidpointRounding.AwayFromZero);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetTax(decimal percentage)
    {{
        if (percentage < 0 || percentage > 100)
        {{
            throw new ArgumentOutOfRangeException(nameof(percentage), ""Percentage must be between 0 and 100"");
        }}
        
        TaxPercentage = decimal.Round(percentage, 2, MidpointRounding.AwayFromZero);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetSalesFee(decimal percentage)
    {{
        if (percentage < 0 || percentage > 100)
        {{
            throw new ArgumentOutOfRangeException(nameof(percentage), ""Percentage must be between 0 and 100"");
        }}
        
        SalesFeePercentage = decimal.Round(percentage, 2, MidpointRounding.AwayFromZero);
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void SetMinimumWithdrawal(decimal amount)
    {{
        if (amount < 0)
        {{
            throw new ArgumentOutOfRangeException(nameof(amount));
        }}
        
        MinimumWithdrawalAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        UpdateDate = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    // ==================== Supporting Entities ====================
    
    public string GetProductImageEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class ProductImage : Entity
{{
    public Guid ProductId {{ get; private set; }}
    public string Path {{ get; private set; }} = string.Empty;
    public int DisplayOrder {{ get; private set; }}

    [SetsRequiredMembers]
    private ProductImage() {{ }}

    [SetsRequiredMembers]
    public ProductImage(Guid productId, string path, int displayOrder)
    {{
        ProductId = productId;
        Path = path;
        DisplayOrder = displayOrder;
    }}
}}
";
    }

    public string GetProductExecutionStepEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class ProductExecutionStep : Entity
{{
    public Guid ProductId {{ get; private set; }}
    public string Title {{ get; private set; }} = string.Empty;
    public string? Description {{ get; private set; }}
    public string? Duration {{ get; private set; }}
    public int DisplayOrder {{ get; private set; }}

    [SetsRequiredMembers]
    private ProductExecutionStep() {{ }}

    [SetsRequiredMembers]
    public ProductExecutionStep(Guid productId, string title, string? description, string? duration, int displayOrder)
    {{
        ProductId = productId;
        Title = title;
        Description = description;
        Duration = duration;
        DisplayOrder = displayOrder;
    }}
}}
";
    }

    public string GetProductFaqEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class ProductFaq : Entity
{{
    public Guid ProductId {{ get; private set; }}
    public string Question {{ get; private set; }} = string.Empty;
    public string Answer {{ get; private set; }} = string.Empty;
    public int DisplayOrder {{ get; private set; }}

    [SetsRequiredMembers]
    private ProductFaq() {{ }}

    [SetsRequiredMembers]
    public ProductFaq(Guid productId, string question, string answer, int displayOrder)
    {{
        ProductId = productId;
        Question = question;
        Answer = answer;
        DisplayOrder = displayOrder;
    }}
}}
";
    }

    public string GetProductCommentEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class ProductComment : Entity
{{
    public Guid ProductId {{ get; private set; }}
    public string AuthorName {{ get; private set; }} = string.Empty;
    public string Content {{ get; private set; }} = string.Empty;
    public double Rating {{ get; private set; }}
    public Guid? ParentId {{ get; private set; }}
    public ProductComment? Parent {{ get; private set; }}
    public bool IsApproved {{ get; private set; }}

    [SetsRequiredMembers]
    private ProductComment() {{ }}

    [SetsRequiredMembers]
    public ProductComment(
        Guid productId,
        string authorName,
        string content,
        double rating,
        ProductComment? parent = null,
        bool isApproved = false)
    {{
        ProductId = productId;
        AuthorName = authorName;
        Content = content;
        Rating = Math.Clamp(rating, 0, 5);
        Parent = parent;
        ParentId = parent?.Id;
        IsApproved = isApproved;
    }}

    public void Approve()
    {{
        IsApproved = true;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void Reject()
    {{
        IsApproved = false;
        UpdateDate = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    public string GetBlogCommentEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class BlogComment : Entity
{{
    public Guid BlogId {{ get; private set; }}
    public string AuthorName {{ get; private set; }} = string.Empty;
    public string? AuthorEmail {{ get; private set; }}
    public string Content {{ get; private set; }} = string.Empty;
    public Guid? ParentId {{ get; private set; }}
    public BlogComment? Parent {{ get; private set; }}
    public bool IsApproved {{ get; private set; }}

    [SetsRequiredMembers]
    private BlogComment() {{ }}

    [SetsRequiredMembers]
    public BlogComment(
        Guid blogId,
        string authorName,
        string? authorEmail,
        string content,
        BlogComment? parent = null,
        bool isApproved = false)
    {{
        BlogId = blogId;
        AuthorName = authorName;
        AuthorEmail = authorEmail;
        Content = content;
        Parent = parent;
        ParentId = parent?.Id;
        IsApproved = isApproved;
    }}

    public void Approve()
    {{
        IsApproved = true;
        UpdateDate = DateTimeOffset.UtcNow;
    }}

    public void Reject()
    {{
        IsApproved = false;
        UpdateDate = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    public string GetInvoiceItemEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class InvoiceItem : Entity
{{
    public Guid InvoiceId {{ get; private set; }}
    public string Description {{ get; private set; }} = string.Empty;
    public decimal UnitPrice {{ get; private set; }}
    public int Quantity {{ get; private set; }}
    public decimal Total => UnitPrice * Quantity;

    [SetsRequiredMembers]
    private InvoiceItem() {{ }}

    [SetsRequiredMembers]
    public InvoiceItem(Guid invoiceId, string description, decimal unitPrice, int quantity)
    {{
        if (string.IsNullOrWhiteSpace(description))
        {{
            throw new ArgumentException(""Description cannot be empty"", nameof(description));
        }}
        
        if (unitPrice < 0)
        {{
            throw new ArgumentOutOfRangeException(nameof(unitPrice));
        }}
        
        if (quantity <= 0)
        {{
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }}
        
        InvoiceId = invoiceId;
        Description = description.Trim();
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
        Quantity = quantity;
    }}
}}
";
    }

    public string GetTransactionEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;
using {_namespace}.Domain.Enums;

namespace {_namespace}.Domain.Entities;

public sealed class Transaction : Entity
{{
    public Guid InvoiceId {{ get; private set; }}
    public string UserId {{ get; private set; }} = string.Empty;
    public decimal Amount {{ get; private set; }}
    public Currency Currency {{ get; private set; }}
    public string Gateway {{ get; private set; }} = string.Empty;
    public string? TransactionId {{ get; private set; }}
    public bool IsSuccessful {{ get; private set; }}
    public string? Notes {{ get; private set; }}
    public DateTimeOffset TransactionDate {{ get; private set; }}

    [SetsRequiredMembers]
    private Transaction() {{ }}

    [SetsRequiredMembers]
    public Transaction(
        Guid invoiceId,
        string userId,
        decimal amount,
        Currency currency,
        string gateway,
        string? transactionId,
        bool isSuccessful,
        string? notes)
    {{
        InvoiceId = invoiceId;
        UserId = userId;
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
        Gateway = gateway;
        TransactionId = transactionId;
        IsSuccessful = isSuccessful;
        Notes = notes;
        TransactionDate = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    public string GetWalletTransactionEntityTemplate()
    {
        return $@"using System;
using System.Diagnostics.CodeAnalysis;
using {_namespace}.Domain.Base;

namespace {_namespace}.Domain.Entities;

public sealed class WalletTransaction : Entity
{{
    public Guid WalletAccountId {{ get; private set; }}
    public decimal Amount {{ get; private set; }}
    public string Description {{ get; private set; }} = string.Empty;
    public string? ReferenceId {{ get; private set; }}
    public DateTimeOffset TransactionDate {{ get; private set; }}

    [SetsRequiredMembers]
    private WalletTransaction() {{ }}

    [SetsRequiredMembers]
    public WalletTransaction(Guid walletAccountId, decimal amount, string description, string? referenceId)
    {{
        if (string.IsNullOrWhiteSpace(description))
        {{
            throw new ArgumentException(""Description cannot be empty"", nameof(description));
        }}
        
        WalletAccountId = walletAccountId;
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Description = description.Trim();
        ReferenceId = referenceId;
        TransactionDate = DateTimeOffset.UtcNow;
    }}
}}
";
    }

    // ==================== Enums ====================
    
    public string GetProductTypeEnumTemplate()
    {
        return $@"namespace {_namespace}.Domain.Enums;

public enum ProductType
{{
    Physical = 0,
    Digital = 1,
    Service = 2
}}
";
    }

    public string GetBlogStatusEnumTemplate()
    {
        return $@"namespace {_namespace}.Domain.Enums;

public enum BlogStatus
{{
    Draft = 0,
    Published = 1,
    Archived = 2
}}
";
    }

    public string GetCategoryScopeEnumTemplate()
    {
        return $@"namespace {_namespace}.Domain.Enums;

public enum CategoryScope
{{
    Product = 0,
    Blog = 1,
    General = 2
}}
";
    }

    public string GetInvoiceStatusEnumTemplate()
    {
        return $@"namespace {_namespace}.Domain.Enums;

public enum InvoiceStatus
{{
    Draft = 0,
    Issued = 1,
    Paid = 2,
    Cancelled = 3,
    Overdue = 4
}}
";
    }

    public string GetDiscountTypeEnumTemplate()
    {
        return $@"namespace {_namespace}.Domain.Enums;

public enum DiscountType
{{
    Percentage = 0,
    FixedAmount = 1
}}
";
    }

    public string GetCurrencyEnumTemplate()
    {
        return $@"namespace {_namespace}.Domain.Enums;

public enum Currency
{{
    IRR = 0,
    USD = 1,
    EUR = 2
}}
";
    }
}
