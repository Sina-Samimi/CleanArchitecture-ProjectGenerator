using System;
using System.Collections.Generic;

namespace MobiRooz.Application.DTOs.Visits;

public sealed record VisitStatisticsDto(
    int TotalVisits,
    int UniqueVisitors,
    int TotalPageVisits,
    DateTime? FirstVisitDate,
    DateTime? LastVisitDate);

public sealed record DailyVisitDto(
    DateOnly Date,
    int VisitCount,
    int UniqueVisitorCount);

public sealed record PageVisitSummaryDto(
    Guid? PageId,
    string? PageTitle,
    string? PageSlug,
    int TotalVisits,
    int UniqueVisitors,
    DateOnly? LastVisitDate);

