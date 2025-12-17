using System;
using System.Collections.Generic;

namespace LogsDtoCloneTest.WebSite.Models.Blog;

public class BlogPostSummaryViewModel
{
    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required string HeroImageUrl { get; init; }

    public required DateTime PublishedAt { get; init; }

    public required int ReadingTimeMinutes { get; init; }

    public required string AuthorName { get; init; }

    public string? AuthorRole { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public string GetFormattedPublishDate() => PublishedAt.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("fa-IR"));

    public string GetReadingTimeLabel() => $"{ReadingTimeMinutes} دقیقه مطالعه";
}
