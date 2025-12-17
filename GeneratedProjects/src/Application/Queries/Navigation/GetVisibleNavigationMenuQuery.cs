using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Navigation;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Navigation;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Navigation;

public sealed record GetVisibleNavigationMenuQuery : IQuery<IReadOnlyList<NavigationMenuItemDto>>
{
    public sealed class Handler : IQueryHandler<GetVisibleNavigationMenuQuery, IReadOnlyList<NavigationMenuItemDto>>
    {
        private readonly INavigationMenuRepository _repository;

        public Handler(INavigationMenuRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyList<NavigationMenuItemDto>>> Handle(
            GetVisibleNavigationMenuQuery request,
            CancellationToken cancellationToken)
        {
            var items = await _repository.GetAllAsync(cancellationToken);

            // Filter only visible items
            var visibleItems = items.Where(item => item.IsVisible).ToList();

            if (visibleItems.Count == 0)
            {
                return Result<IReadOnlyList<NavigationMenuItemDto>>.Success(Array.Empty<NavigationMenuItemDto>());
            }

            var lookup = visibleItems
                .Where(item => item.ParentId.HasValue)
                .GroupBy(item => item.ParentId!.Value)
                .ToDictionary(group => group.Key, group => group
                    .OrderBy(item => item.DisplayOrder)
                    .ThenBy(item => item.Title)
                    .ToList());

            var roots = visibleItems
                .Where(item => !item.ParentId.HasValue)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Title)
                .ToList();

            var result = roots
                .Select(item => Map(item, lookup))
                .ToList();

            return Result<IReadOnlyList<NavigationMenuItemDto>>.Success(result);
        }

        private static NavigationMenuItemDto Map(
            NavigationMenuItem item,
            IReadOnlyDictionary<Guid, List<NavigationMenuItem>> lookup)
        {
            var childDtos = lookup.TryGetValue(item.Id, out var children)
                ? children.Select(child => Map(child, lookup)).ToList()
                : new List<NavigationMenuItemDto>();

            return new NavigationMenuItemDto(
                item.Id,
                item.ParentId,
                item.Title,
                item.Url,
                item.Icon,
                item.ImageUrl,
                item.IsVisible,
                item.OpenInNewTab,
                item.DisplayOrder,
                childDtos);
        }
    }
}

