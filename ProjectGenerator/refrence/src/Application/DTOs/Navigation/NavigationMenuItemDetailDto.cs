using System;

namespace Arsis.Application.DTOs.Navigation;

public sealed record NavigationMenuItemDetailDto(
    Guid Id,
    Guid? ParentId,
    string Title,
    string Url,
    string Icon,
    bool IsVisible,
    bool OpenInNewTab,
    int DisplayOrder);
