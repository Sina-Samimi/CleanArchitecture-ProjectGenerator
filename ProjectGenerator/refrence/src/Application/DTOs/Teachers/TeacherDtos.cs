using System;
using System.Collections.Generic;

namespace Arsis.Application.DTOs.Teachers;

public sealed record TeacherProfileListItemDto(
    Guid Id,
    string DisplayName,
    string? Degree,
    string? Specialty,
    string? Bio,
    string? ContactEmail,
    string? ContactPhone,
    string? UserId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TeacherProfileDetailDto(
    Guid Id,
    string DisplayName,
    string? Degree,
    string? Specialty,
    string? Bio,
    string? AvatarUrl,
    string? ContactEmail,
    string? ContactPhone,
    string? UserId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record TeacherLookupDto(
    Guid Id,
    string DisplayName,
    string? Degree,
    string? UserId,
    bool IsActive);

public sealed record TeacherProfileListResultDto(
    IReadOnlyCollection<TeacherProfileListItemDto> Items,
    int ActiveCount,
    int InactiveCount);
