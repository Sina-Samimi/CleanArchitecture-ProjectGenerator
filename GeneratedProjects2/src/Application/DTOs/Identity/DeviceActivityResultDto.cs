using System.Collections.Generic;

namespace LogsDtoCloneTest.Application.DTOs.Identity;

public sealed record DeviceActivityResultDto(
    IReadOnlyCollection<DeviceActivityDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

