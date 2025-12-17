using System;
using System.Collections.Generic;
using LogsDtoCloneTest.Application.DTOs.Seo;
using LogsDtoCloneTest.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed record SeoMetadataIndexViewModel(
    IReadOnlyCollection<SeoMetadataListItemViewModel> Items,
    int TotalCount,
    SeoPageType? FilterPageType);

public sealed record SeoMetadataListItemViewModel(
    Guid Id,
    SeoPageType PageType,
    string? PageIdentifier,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaRobots,
    DateTimeOffset UpdateDate);

public sealed class SeoMetadataFormViewModel
{
    public Guid? Id { get; set; }
    public SeoPageType PageType { get; set; }
    public string? PageIdentifier { get; set; }
    public bool UseTemplate { get; set; }
    public string? TitleTemplate { get; set; }
    public string? DescriptionTemplate { get; set; }
    public string? OgTitleTemplate { get; set; }
    public string? OgDescriptionTemplate { get; set; }
    public string? RobotsTemplate { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? MetaRobots { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? OgTitle { get; set; }
    public string? OgDescription { get; set; }
    public string? OgImage { get; set; }
    public string? OgType { get; set; }
    public string? OgUrl { get; set; }
    public string? TwitterCard { get; set; }
    public string? TwitterTitle { get; set; }
    public string? TwitterDescription { get; set; }
    public string? TwitterImage { get; set; }
    public string? SchemaJson { get; set; }
    public string? BreadcrumbsJson { get; set; }
    public decimal? SitemapPriority { get; set; }
    public string? SitemapChangefreq { get; set; }
    public string? H1Title { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public string? FeaturedImageAlt { get; set; }
    public string? Tags { get; set; }
    public string? Description { get; set; }
    public SeoFormSelections? Selections { get; set; }
}

public sealed class SeoFormSelections
{
    public List<SelectListItem> PageTypes { get; set; } = new();
    public List<SelectListItem> RobotsOptions { get; set; } = new();
    public List<SelectListItem> OgTypes { get; set; } = new();
    public List<SelectListItem> TwitterCards { get; set; } = new();
    public List<SelectListItem> SitemapChangefreqOptions { get; set; } = new();
}

public sealed class PageFaqFormViewModel
{
    public Guid? Id { get; set; }
    public SeoPageType PageType { get; set; }
    public string? PageIdentifier { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public sealed record PageFaqListViewModel(
    SeoPageType PageType,
    string? PageIdentifier,
    IReadOnlyCollection<PageFaqDto> Faqs);

