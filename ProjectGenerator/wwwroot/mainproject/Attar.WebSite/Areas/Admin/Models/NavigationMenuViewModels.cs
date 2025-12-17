using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Attar.Application.Commands.Admin.NavigationMenu;
using Attar.Application.DTOs.Navigation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Attar.WebSite.Areas.Admin.Models;

public sealed class NavigationMenuPageViewModel
{
    public IReadOnlyCollection<NavigationMenuItemListItemViewModel> Items { get; set; } = Array.Empty<NavigationMenuItemListItemViewModel>();

    public NavigationMenuFilterViewModel Filter { get; set; } = new();

    public int TotalCount { get; set; }

    public int FilteredCount { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalPages { get; set; }

    public IReadOnlyList<SelectListItem> ParentOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<NavigationMenuItemViewModel> TreeItems { get; set; } = Array.Empty<NavigationMenuItemViewModel>();
}

public sealed class NavigationMenuItemListItemViewModel
{
    public Guid Id { get; init; }

    public Guid? ParentId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string Icon { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }

    public bool IsVisible { get; init; }

    public bool OpenInNewTab { get; init; }

    public int DisplayOrder { get; init; }

    public string? ParentTitle { get; init; }

    public int ChildrenCount { get; init; }
}

public sealed class NavigationMenuFilterViewModel
{
    public string? Search { get; set; }

    public Guid? ParentId { get; set; }

    public bool? IsVisible { get; set; }

    [Range(1, 100, ErrorMessage = "تعداد نمایش در صفحه باید بین ۱ تا ۱۰۰ باشد.")]
    public int PageSize { get; set; } = 20;
}

public sealed class NavigationMenuItemViewModel
{
    public Guid Id { get; init; }

    public Guid? ParentId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string Icon { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }

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
            ImageUrl = dto.ImageUrl,
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
    public string? Icon { get; set; }

    [Display(Name = "تصویر (اختیاری - حداکثر 100KB)")]
    public IFormFile? ImageFile { get; set; }

    public string? ImageUrl { get; set; }

    [Display(Name = "نمایش در منو")]
    public bool IsVisible { get; set; } = true;

    [Display(Name = "باز شدن در تب جدید")]
    public bool OpenInNewTab { get; set; }

    [Display(Name = "ترتیب نمایش")]
    [Range(0, int.MaxValue, ErrorMessage = "ترتیب نمایش باید عددی مثبت باشد.")]
    public int DisplayOrder { get; set; }

    [Display(Name = "آیتم والد")]
    public Guid? ParentId { get; set; }

    // Note: ImageUrl is set by controller after file upload
    public CreateNavigationMenuItemCommand ToCreateCommand(string? imageUrl = null)
        => new(
            Title,
            Url,
            Icon ?? string.Empty,
            IsVisible,
            OpenInNewTab,
            DisplayOrder,
            ParentId,
            imageUrl ?? ImageUrl);

    // Note: ImageUrl is set by controller after file upload
    public UpdateNavigationMenuItemCommand ToUpdateCommand(string? imageUrl = null)
    {
        if (!Id.HasValue)
        {
            throw new InvalidOperationException("برای به‌روزرسانی باید شناسه آیتم مشخص باشد.");
        }

        return new UpdateNavigationMenuItemCommand(
            Id.Value,
            Title,
            Url,
            Icon ?? string.Empty,
            IsVisible,
            OpenInNewTab,
            DisplayOrder,
            ParentId,
            imageUrl ?? ImageUrl);
    }

    public static NavigationMenuItemFormViewModel FromDto(NavigationMenuItemDetailDto dto)
        => new()
        {
            Id = dto.Id,
            ParentId = dto.ParentId,
            Title = dto.Title,
            Url = dto.Url,
            Icon = string.IsNullOrWhiteSpace(dto.Icon) ? null : dto.Icon,
            ImageUrl = dto.ImageUrl,
            IsVisible = dto.IsVisible,
            OpenInNewTab = dto.OpenInNewTab,
            DisplayOrder = dto.DisplayOrder
        };
}

public sealed class NavigationMenuFormModalViewModel
{
    public NavigationMenuItemFormViewModel Form { get; set; } = new();

    public IReadOnlyList<SelectListItem> ParentOptions { get; set; } = Array.Empty<SelectListItem>();
}
