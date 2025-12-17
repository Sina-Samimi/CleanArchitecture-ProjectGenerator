using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Navigation;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Admin.NavigationMenu;

public sealed record GetNavigationMenuItemQuery(Guid Id) : IQuery<NavigationMenuItemDetailDto>
{
    public sealed class Handler : IQueryHandler<GetNavigationMenuItemQuery, NavigationMenuItemDetailDto>
    {
        private readonly INavigationMenuRepository _repository;

        public Handler(INavigationMenuRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<NavigationMenuItemDetailDto>> Handle(
            GetNavigationMenuItemQuery request,
            CancellationToken cancellationToken)
        {
            var item = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (item is null)
            {
                return Result<NavigationMenuItemDetailDto>.Failure("آیتم منو یافت نشد.");
            }

            var dto = new NavigationMenuItemDetailDto(
                item.Id,
                item.ParentId,
                item.Title,
                item.Url,
                item.Icon,
                item.ImageUrl,
                item.IsVisible,
                item.OpenInNewTab,
                item.DisplayOrder);

            return Result<NavigationMenuItemDetailDto>.Success(dto);
        }
    }
}
