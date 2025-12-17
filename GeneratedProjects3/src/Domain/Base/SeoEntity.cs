using System;
using System.Diagnostics.CodeAnalysis;

namespace LogTableRenameTest.Domain.Base;

public abstract class SeoEntity : Entity
{
    public string SeoTitle { get; private set; }

    public string SeoDescription { get; private set; }

    public string SeoKeywords { get; private set; }

    public string SeoSlug { get; private set; }

    public string Robots { get; private set; }

    [SetsRequiredMembers]
    protected SeoEntity()
    {
        SeoTitle = string.Empty;
        SeoDescription = string.Empty;
        SeoKeywords = string.Empty;
        SeoSlug = string.Empty;
        Robots = string.Empty;
    }

    protected void UpdateSeo(string seoTitle, string seoDescription, string seoKeywords, string seoSlug, string? robots = null)
    {
        SeoTitle = (seoTitle ?? string.Empty).Trim();
        SeoDescription = (seoDescription ?? string.Empty).Trim();
        SeoKeywords = (seoKeywords ?? string.Empty).Trim();
        SeoSlug = (seoSlug ?? string.Empty).Trim();
        Robots = NormalizeRobots(robots);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    private static string NormalizeRobots(string? robots)
    {
        if (string.IsNullOrWhiteSpace(robots))
        {
            return string.Empty;
        }

        return robots.Trim();
    }
}
