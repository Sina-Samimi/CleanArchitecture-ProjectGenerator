using System.Collections.Generic;

namespace LogTableRenameTest.Application.DTOs.Visits;

public sealed record SiteVisitListResultDto(
    IReadOnlyCollection<SiteVisitListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

