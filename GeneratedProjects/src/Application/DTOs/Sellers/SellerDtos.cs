using System;
using System.Collections.Generic;

namespace TestAttarClone.Application.DTOs.Sellers;

public sealed record SellerProfileListItemDto(
    Guid Id,
    string DisplayName,
    string? LicenseNumber,
    DateOnly? LicenseIssueDate,
    DateOnly? LicenseExpiryDate,
    string? ShopAddress,
    string? WorkingHours,
    int? ExperienceYears,
    string? Bio,
    string? ContactEmail,
    string? ContactPhone,
    string? UserId,
    bool IsActive,
    decimal? SellerSharePercentage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SellerProfileDetailDto(
    Guid Id,
    string DisplayName,
    string? LicenseNumber,
    DateOnly? LicenseIssueDate,
    DateOnly? LicenseExpiryDate,
    string? ShopAddress,
    string? WorkingHours,
    int? ExperienceYears,
    string? Bio,
    string? AvatarUrl,
    string? ContactEmail,
    string? ContactPhone,
    string? UserId,
    bool IsActive,
    decimal? SellerSharePercentage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SellerLookupDto(
    Guid Id,
    string DisplayName,
    string? LicenseNumber,
    string? UserId,
    bool IsActive);

public sealed record SellerProfileListResultDto(
    IReadOnlyCollection<SellerProfileListItemDto> Items,
    int ActiveCount,
    int InactiveCount);
