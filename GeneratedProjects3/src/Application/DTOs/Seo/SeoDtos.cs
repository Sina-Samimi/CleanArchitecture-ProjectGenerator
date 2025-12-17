using System;
using System.Collections.Generic;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.Application.DTOs.Seo;

public sealed record SeoMetadataDto(
    Guid Id,
    SeoPageType PageType,
    string? PageIdentifier,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? MetaRobots,
    string? CanonicalUrl,
    bool UseTemplate,
    string? TitleTemplate,
    string? DescriptionTemplate,
    string? OgTitleTemplate,
    string? OgDescriptionTemplate,
    string? RobotsTemplate,
    string? OgTitle,
    string? OgDescription,
    string? OgImage,
    string? OgType,
    string? OgUrl,
    string? TwitterCard,
    string? TwitterTitle,
    string? TwitterDescription,
    string? TwitterImage,
    string? SchemaJson,
    string? BreadcrumbsJson,
    decimal? SitemapPriority,
    string? SitemapChangefreq,
    string? H1Title,
    string? FeaturedImageUrl,
    string? FeaturedImageAlt,
    string? Tags,
    string? Description,
    DateTimeOffset CreateDate,
    DateTimeOffset UpdateDate);

public sealed record SeoOgImageDto(
    Guid Id,
    Guid SeoMetadataId,
    string ImageUrl,
    int? Width,
    int? Height,
    string? ImageType,
    string? Alt,
    int DisplayOrder,
    DateTimeOffset CreateDate,
    DateTimeOffset UpdateDate);

public sealed record SeoMetadataListItemDto(
    Guid Id,
    SeoPageType PageType,
    string? PageIdentifier,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaRobots,
    DateTimeOffset UpdateDate);

public sealed record SeoMetadataListResultDto(
    IReadOnlyCollection<SeoMetadataListItemDto> Items,
    int TotalCount);

public sealed record PageFaqDto(
    Guid Id,
    SeoPageType PageType,
    string? PageIdentifier,
    string Question,
    string Answer,
    int DisplayOrder,
    DateTimeOffset CreateDate,
    DateTimeOffset UpdateDate);

public sealed record PageFaqListDto(
    IReadOnlyCollection<PageFaqDto> Faqs);

