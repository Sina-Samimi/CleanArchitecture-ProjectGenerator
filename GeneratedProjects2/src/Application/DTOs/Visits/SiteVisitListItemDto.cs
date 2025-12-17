using System;

namespace LogsDtoCloneTest.Application.DTOs.Visits;

public sealed record SiteVisitListItemDto(
    Guid Id,
    string IpAddress,
    DateOnly VisitDate,
    string? UserAgent,
    string? Referrer,
    DateTimeOffset UpdateDate,
    string? DeviceType = null,
    string? OperatingSystem = null,
    string? OsVersion = null,
    string? Browser = null,
    string? BrowserVersion = null,
    string? Engine = null);

