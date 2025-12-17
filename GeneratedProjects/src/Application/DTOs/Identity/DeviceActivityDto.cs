using System;

namespace TestAttarClone.Application.DTOs.Identity;

public sealed record DeviceActivityDto(
    string DeviceKey,
    string DeviceType,
    string ClientName,
    string? IpAddress,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    bool IsActive,
    int ActiveSessionCount,
    int TotalSessionCount);

