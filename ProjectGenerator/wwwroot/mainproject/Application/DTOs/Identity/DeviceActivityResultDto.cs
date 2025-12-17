using System.Collections.Generic;

namespace Attar.Application.DTOs.Identity;

public sealed record DeviceActivityResultDto(
    IReadOnlyCollection<DeviceActivityDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

