using System;

namespace MobiRooz.Application.DTOs.Banners;

public sealed record BannerDto(
    Guid Id,
    string Title,
    string ImagePath,
    string? LinkUrl,
    string? AltText,
    int DisplayOrder,
    bool IsActive,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    bool ShowOnHomePage,
    DateTimeOffset CreateDate,
    DateTimeOffset UpdateDate);

public sealed record BannerListResultDto(
    IReadOnlyCollection<BannerDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

