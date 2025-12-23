using System;
using System.Collections.Generic;
using MobiRooz.Domain.Enums;

namespace MobiRooz.Application.DTOs.Blogs;

public sealed record BlogListItemDto(
    Guid Id,
    string Title,
    string Category,
    Guid CategoryId,
    string Author,
    Guid AuthorId,
    BlogStatus Status,
    DateTimeOffset? PublishedAt,
    int ReadingTimeMinutes,
    int LikeCount,
    int DislikeCount,
    int CommentCount,
    int ViewCount,
    DateTimeOffset UpdatedAt,
    string? FeaturedImagePath,
    string Robots,
    string TagList);

public sealed record BlogStatisticsDto(
    int TotalBlogs,
    int PublishedBlogs,
    int DraftBlogs,
    int TrashBlogs,
    int TotalLikes,
    int TotalDislikes,
    int TotalViews,
    double AverageReadingTimeMinutes);

public sealed record BlogListResultDto(
    IReadOnlyCollection<BlogListItemDto> Items,
    int TotalCount,
    int FilteredCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    BlogStatisticsDto Statistics);

public sealed record BlogCategoryDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    string? Slug,
    string? Description,
    int Depth,
    IReadOnlyCollection<BlogCategoryDto> Children);

public sealed record BlogAuthorDto(
    Guid Id,
    string DisplayName,
    bool IsActive);

public sealed record BlogAuthorListItemDto(
    Guid Id,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    bool IsActive,
    string? UserId,
    string? UserFullName,
    string? UserEmail,
    string? UserPhoneNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record BlogAuthorListResultDto(
    IReadOnlyCollection<BlogAuthorListItemDto> Authors,
    int ActiveCount,
    int InactiveCount);

public sealed record BlogSummaryDto(
    Guid Id,
    string Title,
    string SeoSlug);

public sealed record BlogCommentDto(
    Guid Id,
    Guid BlogId,
    Guid? ParentId,
    string AuthorName,
    string? AuthorEmail,
    string Content,
    bool IsApproved,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? ApprovedById,
    string? ApprovedByName,
    DateTimeOffset? ApprovedAt);

public sealed record BlogCommentListResultDto(
    BlogSummaryDto Blog,
    IReadOnlyCollection<BlogCommentDto> Comments);

public sealed record BlogDetailDto(
    Guid Id,
    string Title,
    string Summary,
    string Content,
    Guid CategoryId,
    Guid AuthorId,
    BlogStatus Status,
    int ReadingTimeMinutes,
    DateTimeOffset? PublishedAt,
    string SeoTitle,
    string SeoDescription,
    string SeoKeywords,
    string SeoSlug,
    int LikeCount,
    int DislikeCount,
    int ViewCount,
    int CommentCount,
    string? FeaturedImagePath,
    string Robots,
    string TagList);

public sealed record BlogLookupsDto(
    IReadOnlyCollection<BlogCategoryDto> Categories,
    IReadOnlyCollection<BlogAuthorDto> Authors);
