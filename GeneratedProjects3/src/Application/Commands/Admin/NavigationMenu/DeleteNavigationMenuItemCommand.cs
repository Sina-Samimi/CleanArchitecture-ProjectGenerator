using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Admin.NavigationMenu;

public sealed record DeleteNavigationMenuItemCommand(Guid Id) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteNavigationMenuItemCommand>
    {
        private readonly INavigationMenuRepository _repository;

        public Handler(INavigationMenuRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result> Handle(DeleteNavigationMenuItemCommand request, CancellationToken cancellationToken)
        {
            var menuItem = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (menuItem is null)
            {
                return Result.Failure("آیتم منو یافت نشد.");
            }

            var hasChildren = await _repository.HasChildrenAsync(request.Id, cancellationToken);

            if (hasChildren)
            {
                return Result.Failure("برای حذف آیتم ابتدا زیرمنوهای آن را حذف کنید.");
            }

            await _repository.RemoveAsync(menuItem, cancellationToken);

            return Result.Success();
        }
    }
}
