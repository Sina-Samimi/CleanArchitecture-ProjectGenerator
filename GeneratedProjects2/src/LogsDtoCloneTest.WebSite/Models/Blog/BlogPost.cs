using System;
using System.Collections.Generic;

namespace LogsDtoCloneTest.WebSite.Models.Blog;

public class BlogPost
{
    public Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required string HeroImageUrl { get; init; }

    public required string AuthorName { get; init; }

    public string? AuthorRole { get; init; }

    public required DateTime PublishedAt { get; init; }

    public required int ReadingTimeMinutes { get; init; }

    public string? SeoTitle { get; init; }

    public string? SeoDescription { get; init; }

    public string? SeoKeywords { get; init; }

    public string? RobotsDirective { get; init; }

    public int TotalViews { get; set; }

    public int CommentCount { get; set; }

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public IReadOnlyList<BlogContentSection> ContentSections { get; init; } = Array.Empty<BlogContentSection>();

    public string? ContentHtml { get; init; }

    public IReadOnlyList<string> KeyInsights { get; init; } = Array.Empty<string>();

    public string? FeaturedQuote { get; init; }

    public string GetFormattedPublishDate() => PublishedAt.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("fa-IR"));

    public string GetReadingTimeLabel() => $"{ReadingTimeMinutes} دقیقه مطالعه";

    public string GetViewCountLabel() => string.Format(new System.Globalization.CultureInfo("fa-IR"), "{0:N0} بازدید", TotalViews);
}

public class BlogContentSection
{
    public string? Heading { get; init; }

    public string? Body { get; init; }

    public bool IsHighlighted { get; init; }
}
