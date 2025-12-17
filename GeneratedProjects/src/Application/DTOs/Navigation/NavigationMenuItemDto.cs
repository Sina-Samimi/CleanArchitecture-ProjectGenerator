using System;
using System.Collections.Generic;

namespace TestAttarClone.Application.DTOs.Navigation;

public sealed record NavigationMenuItemDto(
    Guid Id,
    Guid? ParentId,
    string Title,
    string Url,
    string Icon,
    string? ImageUrl,
    bool IsVisible,
    bool OpenInNewTab,
    int DisplayOrder,
    IReadOnlyList<NavigationMenuItemDto> Children);
