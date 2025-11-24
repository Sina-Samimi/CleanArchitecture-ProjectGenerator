using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using Arsis.Domain.Base;
using Arsis.Domain.Enums;
using Arsis.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Domain.Entities.Blogs;

public sealed class Blog : SeoEntity, IAggregateRoot
{
    private const int MaxTagLength = 50;

    private readonly List<BlogComment> _comments = new();
    private readonly List<BlogDailyView> _views = new();

    public string Title { get; private set; }

    public string Summary { get; private set; }

    public string Content { get; private set; }

    public int ReadingTimeMinutes { get; private set; }

    public BlogStatus Status { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public Guid CategoryId { get; private set; }

    public BlogCategory Category { get; private set; } = null!;

    public Guid AuthorId { get; private set; }

    public BlogAuthor Author { get; private set; } = null!;

    public int LikeCount { get; private set; }

    public int DislikeCount { get; private set; }

    public string? FeaturedImagePath { get; private set; }

    public string TagList { get; private set; }

    [BackingField(nameof(_comments))]
    public IReadOnlyCollection<BlogComment> Comments => _comments;

    [BackingField(nameof(_views))]
    public IReadOnlyCollection<BlogDailyView> Views => _views;

    public IReadOnlyCollection<string> Tags => ParseTags(TagList);

    [SetsRequiredMembers]
    private Blog()
    {
        Title = string.Empty;
        Summary = string.Empty;
        Content = string.Empty;
        Status = BlogStatus.Draft;
        TagList = string.Empty;
    }

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
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(author);

        UpdateContent(title, summary, content, readingTimeMinutes);
        SetCategory(category);
        SetAuthor(author);
        ChangeStatus(status, publishedAt);
        UpdateSeo(seoTitle, seoDescription, seoKeywords, seoSlug, robots);
        SetFeaturedImage(featuredImagePath);
        SetTags(tags);
    }

    public void UpdateContent(string title, string summary, string content, int readingTimeMinutes)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Blog title cannot be empty", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Blog content cannot be empty", nameof(content));
        }

        Title = title.Trim();
        Summary = (summary ?? string.Empty).Trim();
        Content = content.Trim();
        ReadingTimeMinutes = Math.Max(1, readingTimeMinutes);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetCategory(BlogCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        Category = category;
        CategoryId = category.Id;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetAuthor(BlogAuthor author)
    {
        ArgumentNullException.ThrowIfNull(author);
        Author = author;
        AuthorId = author.Id;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void ChangeStatus(BlogStatus status, DateTimeOffset? publishedAt = null)
    {
        Status = status;
        if (status == BlogStatus.Published)
        {
            PublishedAt = publishedAt ?? DateTimeOffset.UtcNow;
        }
        else if (status == BlogStatus.Draft)
        {
            PublishedAt = null;
        }

        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateSeoMetadata(string seoTitle, string seoDescription, string seoKeywords, string seoSlug, string? robots)
        => UpdateSeo(seoTitle, seoDescription, seoKeywords, seoSlug, robots);

    public void SetFeaturedImage(string? imagePath)
    {
        FeaturedImagePath = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
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

    public void RegisterView(IPAddress viewerIp, DateOnly viewDate)
    {
        viewerIp ??= IPAddress.None;
        var existing = _views.FirstOrDefault(view =>
            view.ViewDate == viewDate &&
            view.ViewerIp.Equals(viewerIp));

        if (existing is null)
        {
            _views.Add(new BlogDailyView(Id, viewerIp, viewDate));
        }
        else
        {
            existing.UpdateViewDate(viewDate);
        }

        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void IncrementLikes()
    {
        LikeCount++;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void IncrementDislikes()
    {
        DislikeCount++;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public BlogComment AddComment(string authorName, string content, string? authorEmail = null, Guid? parentId = null)
    {
        BlogComment? parent = null;
        if (parentId.HasValue)
        {
            parent = _comments.FirstOrDefault(comment => comment.Id == parentId.Value);
        }

        var comment = new BlogComment(Id, authorName, content, authorEmail, parent);
        _comments.Add(comment);
        UpdateDate = DateTimeOffset.UtcNow;
        return comment;
    }

    private static IReadOnlyCollection<string> ParseTags(string tagList)
    {
        if (string.IsNullOrWhiteSpace(tagList))
        {
            return Array.Empty<string>();
        }

        return tagList
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .ToArray();
    }
}
