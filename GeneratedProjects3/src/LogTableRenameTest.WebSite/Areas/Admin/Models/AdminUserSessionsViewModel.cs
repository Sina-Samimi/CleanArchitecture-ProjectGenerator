using System;
using System.Collections.Generic;
using LogTableRenameTest.Application.DTOs.Identity;

namespace LogTableRenameTest.WebSite.Areas.Admin.Models;

public sealed class AdminUserSessionsViewModel
{
    public string UserId { get; init; } = string.Empty;

    public string UserFullName { get; init; } = string.Empty;

    public string UserEmail { get; init; } = string.Empty;

    public IReadOnlyCollection<ActivityEntryDto> Activities { get; init; } = Array.Empty<ActivityEntryDto>();

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public string? FilterActivityType { get; init; }

    public string? FilterDeviceType { get; init; }

    public bool? FilterIsActive { get; init; }
}

