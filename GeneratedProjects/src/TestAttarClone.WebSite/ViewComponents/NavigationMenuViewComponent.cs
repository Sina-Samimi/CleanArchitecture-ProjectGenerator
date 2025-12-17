using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestAttarClone.Application.DTOs.Catalog;
using TestAttarClone.Application.DTOs.Navigation;
using TestAttarClone.Application.Queries.Catalog;
using TestAttarClone.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace TestAttarClone.WebSite.ViewComponents;

public sealed class NavigationMenuViewComponent : ViewComponent
{
    private readonly IMediator _mediator;

    public NavigationMenuViewComponent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync(string viewName = "Default")
    {
        var cancellationToken = HttpContext.RequestAborted;
        
        // Get navigation menu items
        var menuQuery = new Application.Queries.Navigation.GetVisibleNavigationMenuQuery();
        var menuResult = await _mediator.Send(menuQuery, cancellationToken);
        
        var menuItems = menuResult.IsSuccess && menuResult.Value is not null
            ? menuResult.Value
            : new List<NavigationMenuItemDto>();

        // Get categories for the fixed "دسته‌بندی" menu
        var categoriesQuery = new GetSiteCategoriesQuery(CategoryScope.Product);
        var categoriesResult = await _mediator.Send(categoriesQuery, cancellationToken);
        
        var categories = categoriesResult.IsSuccess && categoriesResult.Value is not null
            ? categoriesResult.Value
            : Array.Empty<SiteCategoryDto>();

        // Build categories menu item with hierarchical structure
        // Helper function to map category to NavigationMenuItemDto recursively
        NavigationMenuItemDto MapCategoryToMenuItem(SiteCategoryDto category, Guid? parentIdForMenu = null)
        {
            // For top-level categories, set parent to Guid.Empty (the "دسته‌بندی" menu)
            // For nested categories, use the actual parent category ID
            var menuParentId = parentIdForMenu ?? (category.ParentId == null ? Guid.Empty : category.ParentId);
            
            // Map children recursively, passing current category ID as parent
            var children = category.Children
                .Select(child => MapCategoryToMenuItem(child, category.Id))
                .ToList();
            
            return new NavigationMenuItemDto(
                category.Id,
                menuParentId,
                category.Name,
                Url.Action("Index", "Product", new { category = category.Slug }) ?? $"/Product?category={category.Slug}",
                string.Empty,
                category.ImageUrl,
                true,
                false,
                0,
                children);
        }
        
        // Get only top-level categories (no parent) and map them recursively
        // Note: categories already come as a tree structure from GetSiteCategoriesQuery
        // Top-level categories will have ParentId = Guid.Empty (pointing to "دسته‌بندی" menu)
        var categoryChildren = categories
            .OrderBy(c => c.Name)
            .Select(c => MapCategoryToMenuItem(c, Guid.Empty))
            .ToList();

        // Create fixed "دسته‌بندی" menu item with DisplayOrder = -1 to always be first
        var categoriesMenu = new NavigationMenuItemDto(
            Guid.Empty, // Use empty GUID for fixed menu
            null,
            "دسته‌بندی",
            "#",
            string.Empty,
            null,
            true,
            false,
            -1, // Always first
            categoryChildren);

        // Combine: categories menu first, then other menu items
        var allItems = new List<NavigationMenuItemDto> { categoriesMenu };
        allItems.AddRange(menuItems);

        // Sort by DisplayOrder (categories menu with -1 will be first)
        var sortedItems = allItems
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Title)
            .ToList();

        return View(viewName, sortedItems);
    }
}

