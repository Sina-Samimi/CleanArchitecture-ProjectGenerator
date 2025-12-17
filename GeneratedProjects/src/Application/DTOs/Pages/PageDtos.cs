using System;

namespace TestAttarClone.Application.DTOs.Pages;

public sealed record PageDto(
    Guid Id,
    string Title,
    string Slug,
    string Content,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? MetaRobots,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    int ViewCount,
    string? FeaturedImagePath,
    bool ShowInFooter,
    bool ShowInQuickAccess,
    DateTimeOffset CreateDate,
    DateTimeOffset UpdateDate);

public sealed record PageListItemDto(
    Guid Id,
    string Title,
    string Slug,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    int ViewCount,
    DateTimeOffset CreateDate);

public sealed record PageListResultDto(
    IReadOnlyCollection<PageListItemDto> Pages,
    int TotalCount);

public sealed record PageStatisticsDto(
    int TotalPages,
    int PublishedPages,
    int DraftPages,
    int TotalViews,
    int FooterPages,
    int QuickAccessPages);

