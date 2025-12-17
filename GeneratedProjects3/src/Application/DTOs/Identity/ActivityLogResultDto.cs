using System.Collections.Generic;

namespace LogTableRenameTest.Application.DTOs.Identity;

public sealed record ActivityLogResultDto(
    IReadOnlyCollection<ActivityEntryDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

