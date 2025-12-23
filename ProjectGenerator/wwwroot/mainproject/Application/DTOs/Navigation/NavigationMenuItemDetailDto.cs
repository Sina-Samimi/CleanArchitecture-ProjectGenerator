using System;

namespace MobiRooz.Application.DTOs.Navigation;

public sealed record NavigationMenuItemDetailDto(
    Guid Id,
    Guid? ParentId,
    string Title,
    string Url,
    string Icon,
    string? ImageUrl,
    bool IsVisible,
    bool OpenInNewTab,
    int DisplayOrder);
