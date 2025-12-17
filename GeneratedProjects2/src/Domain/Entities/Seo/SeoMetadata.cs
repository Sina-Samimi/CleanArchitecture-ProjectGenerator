using System;
using System.Diagnostics.CodeAnalysis;
using LogsDtoCloneTest.Domain.Base;
using LogsDtoCloneTest.Domain.Enums;

namespace LogsDtoCloneTest.Domain.Entities.Seo;

public sealed class SeoMetadata : Entity
{
    public SeoPageType PageType { get; private set; }

    public string? PageIdentifier { get; private set; } // Slug, Id, Route - برای شناسایی صفحه خاص

    // Meta Tags (Static)
    public string? MetaTitle { get; private set; }

    public string? MetaDescription { get; private set; }

    public string? MetaKeywords { get; private set; }

    public string? MetaRobots { get; private set; } // index,follow, noindex,nofollow, etc.

    public string? CanonicalUrl { get; private set; }

    // Template SEO (Dynamic)
    public bool UseTemplate { get; private set; }

    public string? TitleTemplate { get; private set; } // مثال: "15 تا از بهترین {category} ها در {province} {city} | {siteName}"

    public string? DescriptionTemplate { get; private set; }

    public string? OgTitleTemplate { get; private set; }

    public string? OgDescriptionTemplate { get; private set; }

    public string? RobotsTemplate { get; private set; } // مثال: "{isIndex ? 'index,follow' : 'noindex,follow'}"

    // Open Graph (Static)
    public string? OgTitle { get; private set; }

    public string? OgDescription { get; private set; }

    public string? OgImage { get; private set; }

    public string? OgType { get; private set; } // website, article, product, etc.

    public string? OgUrl { get; private set; }

    // Twitter Card
    public string? TwitterCard { get; private set; } // summary, summary_large_image

    public string? TwitterTitle { get; private set; }

    public string? TwitterDescription { get; private set; }

    public string? TwitterImage { get; private set; }

    // Schema.org JSON-LD (ذخیره به صورت JSON)
    public string? SchemaJson { get; private set; }

    // Breadcrumbs (JSON array)
    public string? BreadcrumbsJson { get; private set; }

    // Priority برای Sitemap
    public decimal? SitemapPriority { get; private set; }

    public string? SitemapChangefreq { get; private set; } // always, hourly, daily, weekly, monthly, yearly, never

    // محتوای صفحه
    public string? H1Title { get; private set; } // عنوان H1 (فقط یک عدد)

    public string? FeaturedImageUrl { get; private set; } // تصویر شاخص (آپلود یا لینک)

    public string? FeaturedImageAlt { get; private set; } // Alt تصویر شاخص

    public string? Tags { get; private set; } // برچسب‌ها (comma separated یا JSON)

    public string? Description { get; private set; } // توضیحات (Rich Text Editor)

    [SetsRequiredMembers]
    private SeoMetadata()
    {
    }

    [SetsRequiredMembers]
    public SeoMetadata(
        SeoPageType pageType,
        string? pageIdentifier = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? metaKeywords = null,
        string? metaRobots = null,
        string? canonicalUrl = null,
        bool useTemplate = false,
        string? titleTemplate = null,
        string? descriptionTemplate = null,
        string? ogTitleTemplate = null,
        string? ogDescriptionTemplate = null,
        string? robotsTemplate = null,
        string? ogTitle = null,
        string? ogDescription = null,
        string? ogImage = null,
        string? ogType = null,
        string? ogUrl = null,
        string? twitterCard = null,
        string? twitterTitle = null,
        string? twitterDescription = null,
        string? twitterImage = null,
        string? schemaJson = null,
        string? breadcrumbsJson = null,
        decimal? sitemapPriority = null,
        string? sitemapChangefreq = null,
        string? h1Title = null,
        string? featuredImageUrl = null,
        string? featuredImageAlt = null,
        string? tags = null,
        string? description = null)
    {
        UpdateSeo(
            pageType,
            pageIdentifier,
            metaTitle,
            metaDescription,
            metaKeywords,
            metaRobots,
            canonicalUrl,
            useTemplate,
            titleTemplate,
            descriptionTemplate,
            ogTitleTemplate,
            ogDescriptionTemplate,
            robotsTemplate,
            ogTitle,
            ogDescription,
            ogImage,
            ogType,
            ogUrl,
            twitterCard,
            twitterTitle,
            twitterDescription,
            twitterImage,
            schemaJson,
            breadcrumbsJson,
            sitemapPriority,
            sitemapChangefreq,
            h1Title,
            featuredImageUrl,
            featuredImageAlt,
            tags,
            description);
    }

    public void UpdateSeo(
        SeoPageType pageType,
        string? pageIdentifier = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? metaKeywords = null,
        string? metaRobots = null,
        string? canonicalUrl = null,
        bool useTemplate = false,
        string? titleTemplate = null,
        string? descriptionTemplate = null,
        string? ogTitleTemplate = null,
        string? ogDescriptionTemplate = null,
        string? robotsTemplate = null,
        string? ogTitle = null,
        string? ogDescription = null,
        string? ogImage = null,
        string? ogType = null,
        string? ogUrl = null,
        string? twitterCard = null,
        string? twitterTitle = null,
        string? twitterDescription = null,
        string? twitterImage = null,
        string? schemaJson = null,
        string? breadcrumbsJson = null,
        decimal? sitemapPriority = null,
        string? sitemapChangefreq = null,
        string? h1Title = null,
        string? featuredImageUrl = null,
        string? featuredImageAlt = null,
        string? tags = null,
        string? description = null)
    {
        PageType = pageType;
        PageIdentifier = string.IsNullOrWhiteSpace(pageIdentifier) ? null : pageIdentifier.Trim();
        UseTemplate = useTemplate;
        TitleTemplate = string.IsNullOrWhiteSpace(titleTemplate) ? null : titleTemplate.Trim();
        DescriptionTemplate = string.IsNullOrWhiteSpace(descriptionTemplate) ? null : descriptionTemplate.Trim();
        OgTitleTemplate = string.IsNullOrWhiteSpace(ogTitleTemplate) ? null : ogTitleTemplate.Trim();
        OgDescriptionTemplate = string.IsNullOrWhiteSpace(ogDescriptionTemplate) ? null : ogDescriptionTemplate.Trim();
        RobotsTemplate = string.IsNullOrWhiteSpace(robotsTemplate) ? null : robotsTemplate.Trim();
        MetaTitle = string.IsNullOrWhiteSpace(metaTitle) ? null : metaTitle.Trim();
        MetaDescription = string.IsNullOrWhiteSpace(metaDescription) ? null : metaDescription.Trim();
        MetaKeywords = string.IsNullOrWhiteSpace(metaKeywords) ? null : metaKeywords.Trim();
        MetaRobots = string.IsNullOrWhiteSpace(metaRobots) ? null : metaRobots.Trim();
        CanonicalUrl = string.IsNullOrWhiteSpace(canonicalUrl) ? null : canonicalUrl.Trim();
        OgTitle = string.IsNullOrWhiteSpace(ogTitle) ? null : ogTitle.Trim();
        OgDescription = string.IsNullOrWhiteSpace(ogDescription) ? null : ogDescription.Trim();
        OgImage = string.IsNullOrWhiteSpace(ogImage) ? null : ogImage.Trim();
        OgType = string.IsNullOrWhiteSpace(ogType) ? null : ogType.Trim();
        OgUrl = string.IsNullOrWhiteSpace(ogUrl) ? null : ogUrl.Trim();
        TwitterCard = string.IsNullOrWhiteSpace(twitterCard) ? null : twitterCard.Trim();
        TwitterTitle = string.IsNullOrWhiteSpace(twitterTitle) ? null : twitterTitle.Trim();
        TwitterDescription = string.IsNullOrWhiteSpace(twitterDescription) ? null : twitterDescription.Trim();
        TwitterImage = string.IsNullOrWhiteSpace(twitterImage) ? null : twitterImage.Trim();
        SchemaJson = string.IsNullOrWhiteSpace(schemaJson) ? null : schemaJson.Trim();
        BreadcrumbsJson = string.IsNullOrWhiteSpace(breadcrumbsJson) ? null : breadcrumbsJson.Trim();
        SitemapPriority = sitemapPriority;
        SitemapChangefreq = string.IsNullOrWhiteSpace(sitemapChangefreq) ? null : sitemapChangefreq.Trim();
        H1Title = string.IsNullOrWhiteSpace(h1Title) ? null : h1Title.Trim();
        FeaturedImageUrl = string.IsNullOrWhiteSpace(featuredImageUrl) ? null : featuredImageUrl.Trim();
        FeaturedImageAlt = string.IsNullOrWhiteSpace(featuredImageAlt) ? null : featuredImageAlt.Trim();
        Tags = string.IsNullOrWhiteSpace(tags) ? null : tags.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }
}

