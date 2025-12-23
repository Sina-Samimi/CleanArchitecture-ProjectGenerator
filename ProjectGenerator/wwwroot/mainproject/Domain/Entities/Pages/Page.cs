using System;
using System.Diagnostics.CodeAnalysis;
using MobiRooz.Domain.Base;

namespace MobiRooz.Domain.Entities.Pages;

public sealed class Page : Entity
{
    public string Title { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public string? MetaTitle { get; private set; }

    public string? MetaDescription { get; private set; }

    public string? MetaKeywords { get; private set; }

    public string? MetaRobots { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public int ViewCount { get; private set; }

    public string? FeaturedImagePath { get; private set; }

    public bool ShowInFooter { get; private set; }

    public bool ShowInQuickAccess { get; private set; }

    [SetsRequiredMembers]
    private Page()
    {
    }

    [SetsRequiredMembers]
    public Page(
        string title,
        string slug,
        string content,
        string? metaTitle = null,
        string? metaDescription = null,
        string? metaKeywords = null,
        string? metaRobots = null,
        bool isPublished = false,
        DateTimeOffset? publishedAt = null,
        string? featuredImagePath = null,
        bool showInFooter = false,
        bool showInQuickAccess = false)
    {
        UpdateContent(title, slug, content);
        UpdateSeo(metaTitle, metaDescription, metaKeywords, metaRobots);
        SetFeaturedImage(featuredImagePath);
        SetDisplayOptions(showInFooter, showInQuickAccess);

        if (isPublished)
        {
            Publish(publishedAt);
        }
        else
        {
            Unpublish();
        }
    }

    public void UpdateContent(string title, string slug, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(slug, nameof(slug));
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        Title = title.Trim();
        Slug = NormalizeSlug(slug);
        Content = content;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateSeo(string? metaTitle, string? metaDescription, string? metaKeywords, string? metaRobots)
    {
        MetaTitle = string.IsNullOrWhiteSpace(metaTitle) ? null : metaTitle.Trim();
        MetaDescription = string.IsNullOrWhiteSpace(metaDescription) ? null : metaDescription.Trim();
        MetaKeywords = string.IsNullOrWhiteSpace(metaKeywords) ? null : metaKeywords.Trim();
        MetaRobots = string.IsNullOrWhiteSpace(metaRobots) ? null : metaRobots.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Publish(DateTimeOffset? publishedAt = null)
    {
        if (IsPublished)
        {
            return;
        }

        IsPublished = true;
        PublishedAt = publishedAt ?? DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Unpublish()
    {
        if (!IsPublished)
        {
            return;
        }

        IsPublished = false;
        PublishedAt = null;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetFeaturedImage(string? featuredImagePath)
    {
        FeaturedImagePath = string.IsNullOrWhiteSpace(featuredImagePath) ? null : featuredImagePath.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetDisplayOptions(bool showInFooter, bool showInQuickAccess)
    {
        ShowInFooter = showInFooter;
        ShowInQuickAccess = showInQuickAccess;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    private static string NormalizeSlug(string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug, nameof(slug));

        var normalized = slug.Trim().ToLowerInvariant();

        // Replace spaces and special characters with hyphens
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^\w\s-]", "");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", "-");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"-+", "-");
        normalized = normalized.Trim('-');

        return normalized;
    }
}

