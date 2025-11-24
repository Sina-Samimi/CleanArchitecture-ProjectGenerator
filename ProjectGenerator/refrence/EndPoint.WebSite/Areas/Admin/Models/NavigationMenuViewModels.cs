using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Arsis.Application.Commands.Admin.NavigationMenu;
using Arsis.Application.DTOs.Navigation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EndPoint.WebSite.Areas.Admin.Models;

public sealed class NavigationMenuPageViewModel
{
    public IReadOnlyList<NavigationMenuItemViewModel> Items { get; set; } = Array.Empty<NavigationMenuItemViewModel>();

    public NavigationMenuItemFormViewModel Form { get; set; } = new();

    public IReadOnlyList<SelectListItem> ParentOptions { get; set; } = Array.Empty<SelectListItem>();

    public bool IsEditMode => Form.Id.HasValue;
}

public sealed class NavigationMenuItemViewModel
{
    public Guid Id { get; init; }

    public Guid? ParentId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string Icon { get; init; } = string.Empty;

    public bool IsVisible { get; init; }

    public bool OpenInNewTab { get; init; }

    public int DisplayOrder { get; init; }

    public IReadOnlyList<NavigationMenuItemViewModel> Children { get; init; }
        = Array.Empty<NavigationMenuItemViewModel>();

    public static NavigationMenuItemViewModel FromDto(NavigationMenuItemDto dto)
        => new()
        {
            Id = dto.Id,
            ParentId = dto.ParentId,
            Title = dto.Title,
            Url = dto.Url,
            Icon = dto.Icon,
            IsVisible = dto.IsVisible,
            OpenInNewTab = dto.OpenInNewTab,
            DisplayOrder = dto.DisplayOrder,
            Children = dto.Children
                .Select(FromDto)
                .ToList()
        };

    public IEnumerable<NavigationMenuItemViewModel> Flatten()
    {
        yield return this;

        foreach (var child in Children)
        {
            foreach (var descendant in child.Flatten())
            {
                yield return descendant;
            }
        }
    }
}

public sealed class NavigationMenuItemFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "عنوان منو")]
    [Required(ErrorMessage = "عنوان منو الزامی است.")]
    [StringLength(200, ErrorMessage = "عنوان منو نمی‌تواند بیشتر از ۲۰۰ کاراکتر باشد.")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "آدرس لینک")]
    [StringLength(500, ErrorMessage = "آدرس لینک نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.")]
    public string Url { get; set; } = string.Empty;

    [Display(Name = "آیکون (اختیاری)")]
    [StringLength(100, ErrorMessage = "آیکون نمی‌تواند بیشتر از ۱۰۰ کاراکتر باشد.")]
    public string Icon { get; set; } = string.Empty;

    [Display(Name = "نمایش در منو")]
    public bool IsVisible { get; set; } = true;

    [Display(Name = "باز شدن در تب جدید")]
    public bool OpenInNewTab { get; set; }

    [Display(Name = "ترتیب نمایش")]
    [Range(0, int.MaxValue, ErrorMessage = "ترتیب نمایش باید عددی مثبت باشد.")]
    public int DisplayOrder { get; set; }

    [Display(Name = "آیتم والد")]
    public Guid? ParentId { get; set; }

    public CreateNavigationMenuItemCommand ToCreateCommand()
        => new(
            Title,
            Url,
            Icon,
            IsVisible,
            OpenInNewTab,
            DisplayOrder,
            ParentId);

    public UpdateNavigationMenuItemCommand ToUpdateCommand()
    {
        if (!Id.HasValue)
        {
            throw new InvalidOperationException("برای به‌روزرسانی باید شناسه آیتم مشخص باشد.");
        }

        return new UpdateNavigationMenuItemCommand(
            Id.Value,
            Title,
            Url,
            Icon,
            IsVisible,
            OpenInNewTab,
            DisplayOrder,
            ParentId);
    }

    public static NavigationMenuItemFormViewModel FromDto(NavigationMenuItemDetailDto dto)
        => new()
        {
            Id = dto.Id,
            ParentId = dto.ParentId,
            Title = dto.Title,
            Url = dto.Url,
            Icon = dto.Icon,
            IsVisible = dto.IsVisible,
            OpenInNewTab = dto.OpenInNewTab,
            DisplayOrder = dto.DisplayOrder
        };
}
