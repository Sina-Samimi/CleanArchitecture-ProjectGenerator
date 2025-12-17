using System;
using System.Collections.Generic;
using TestAttarClone.Application.DTOs.Identity;

namespace TestAttarClone.WebSite.Areas.User.Models;

public sealed class DeviceActivityViewModel
{
    public IReadOnlyCollection<DeviceActivityDto> Devices { get; init; } = Array.Empty<DeviceActivityDto>();

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public string? FilterDeviceType { get; init; }

    public bool? FilterIsActive { get; init; }
}

