using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Navigation;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Entities.Navigation;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Queries.Admin.NavigationMenu;

public sealed record GetNavigationMenuTreeQuery : IQuery<IReadOnlyList<NavigationMenuItemDto>>
{
    public sealed class Handler : IQueryHandler<GetNavigationMenuTreeQuery, IReadOnlyList<NavigationMenuItemDto>>
    {
        private readonly INavigationMenuRepository _repository;

        public Handler(INavigationMenuRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyList<NavigationMenuItemDto>>> Handle(
            GetNavigationMenuTreeQuery request,
            CancellationToken cancellationToken)
        {
            var items = await _repository.GetAllAsync(cancellationToken);

            if (items.Count == 0)
            {
                return Result<IReadOnlyList<NavigationMenuItemDto>>.Success(Array.Empty<NavigationMenuItemDto>());
            }

            var lookup = items
                .Where(item => item.ParentId.HasValue)
                .GroupBy(item => item.ParentId!.Value)
                .ToDictionary(group => group.Key, group => group
                    .OrderBy(item => item.DisplayOrder)
                    .ThenBy(item => item.Title)
                    .ToList());

            var roots = items
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
