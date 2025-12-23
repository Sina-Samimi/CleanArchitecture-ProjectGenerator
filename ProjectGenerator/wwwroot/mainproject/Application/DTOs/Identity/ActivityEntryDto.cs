using System;

namespace MobiRooz.Application.DTOs.Identity;

public sealed record ActivityEntryDto(
    Guid? SessionId,
    string Title,
    DateTimeOffset Timestamp,
    string Context,
    string? DeviceType,
    string? ClientName,
    string? IpAddress,
    bool IsCurrentSession,
    bool IsActive);

