using System.Collections.Generic;

namespace MobiRooz.Application.DTOs.Visits;

public sealed record SiteVisitListResultDto(
    IReadOnlyCollection<SiteVisitListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

