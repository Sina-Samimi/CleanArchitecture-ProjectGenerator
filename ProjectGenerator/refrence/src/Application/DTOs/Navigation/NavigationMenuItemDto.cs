using System;
using System.Collections.Generic;

namespace Arsis.Application.DTOs.Navigation;

public sealed record NavigationMenuItemDto(
    Guid Id,
    Guid? ParentId,
    string Title,
    string Url,
    string Icon,
    bool IsVisible,
    bool OpenInNewTab,
    int DisplayOrder,
    IReadOnlyList<NavigationMenuItemDto> Children);
