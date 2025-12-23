using System.Collections.Generic;

namespace MobiRooz.Application.DTOs.Visits;

public sealed record VisitStatisticsSummaryDto(
    IReadOnlyCollection<DeviceTypeStatDto> DeviceTypeStats,
    IReadOnlyCollection<OperatingSystemStatDto> OperatingSystemStats,
    IReadOnlyCollection<BrowserStatDto> BrowserStats);

public sealed record DeviceTypeStatDto(
    string DeviceType,
    int Count,
    double Percentage);

public sealed record OperatingSystemStatDto(
    string OperatingSystem,
    int Count,
    double Percentage);

public sealed record BrowserStatDto(
    string Browser,
    int Count,
    double Percentage);
