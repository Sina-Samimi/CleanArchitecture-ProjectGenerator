using System;
using System.Collections.Generic;
using Attar.Application.DTOs.Identity;

namespace Attar.WebSite.Areas.User.Models;

public sealed class ActivityLogViewModel
{
    public IReadOnlyCollection<ActivityEntryDto> Activities { get; init; } = Array.Empty<ActivityEntryDto>();

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public string? FilterActivityType { get; init; }

    public string? FilterDeviceType { get; init; }

    public bool? FilterIsActive { get; init; }
}

