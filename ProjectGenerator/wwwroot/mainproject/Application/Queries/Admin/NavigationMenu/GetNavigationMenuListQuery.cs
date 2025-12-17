using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Navigation;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Navigation;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Admin.NavigationMenu;

public sealed record GetNavigationMenuListQuery(
    string? Search,
    Guid? ParentId,
    bool? IsVisible,
    int Page,
    int PageSize) : IQuery<NavigationMenuListResultDto>;

public sealed record NavigationMenuListResultDto(
    IReadOnlyCollection<NavigationMenuItemListItemDto> Items,
    int TotalCount,
    int FilteredCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

public sealed record NavigationMenuItemListItemDto(
    Guid Id,
    Guid? ParentId,
    string Title,
    string Url,
    string Icon,
    string? ImageUrl,
    bool IsVisible,
    bool OpenInNewTab,
    int DisplayOrder,
    string? ParentTitle,
    int ChildrenCount);

public sealed class GetNavigationMenuListQueryHandler : IQueryHandler<GetNavigationMenuListQuery, NavigationMenuListResultDto>
{
    private readonly INavigationMenuRepository _repository;

    public GetNavigationMenuListQueryHandler(INavigationMenuRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<NavigationMenuListResultDto>> Handle(
        GetNavigationMenuListQuery request,
        CancellationToken cancellationToken)
    {
        var allItems = await _repository.GetAllAsync(cancellationToken);
        var totalCount = allItems.Count;

        // Build lookup for parent titles and children count
        var parentLookup = allItems
            .Where(item => item.ParentId.HasValue)
            .GroupBy(item => item.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());

        var parentTitleLookup = allItems
            .ToDictionary(item => item.Id, item => item.Title);

        // Apply filters
        var filtered = allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();
            filtered = filtered.Where(item =>
                item.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(item.Url) && item.Url.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        if (request.ParentId.HasValue)
        {
            filtered = filtered.Where(item => item.ParentId == request.ParentId.Value);
        }
        else if (request.ParentId == Guid.Empty)
        {
            // Empty Guid means root items only
            filtered = filtered.Where(item => !item.ParentId.HasValue);
        }

        if (request.IsVisible.HasValue)
        {
            filtered = filtered.Where(item => item.IsVisible == request.IsVisible.Value);
        }

        var filteredList = filtered.ToList();
        var filteredCount = filteredList.Count;

        // Order by DisplayOrder then Title
        var ordered = filteredList
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Title)
            .ToList();

        // Apply pagination
        var pageNumber = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Clamp(request.PageSize, 5, 100);
        var totalPages = filteredCount == 0 ? 1 : (int)Math.Ceiling(filteredCount / (double)pageSize);

        var pagedItems = ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new NavigationMenuItemListItemDto(
                item.Id,
                item.ParentId,
                item.Title,
                item.Url,
                item.Icon,
                item.ImageUrl,
                item.IsVisible,
                item.OpenInNewTab,
                item.DisplayOrder,
                item.ParentId.HasValue && parentTitleLookup.TryGetValue(item.ParentId.Value, out var parentTitle)
                    ? parentTitle
                    : null,
                parentLookup.TryGetValue(item.Id, out var childrenCount) ? childrenCount : 0))
            .ToList();

        var result = new NavigationMenuListResultDto(
            pagedItems,
            totalCount,
            filteredCount,
            pageNumber,
            pageSize,
            totalPages);

        return Result<NavigationMenuListResultDto>.Success(result);
    }
}
