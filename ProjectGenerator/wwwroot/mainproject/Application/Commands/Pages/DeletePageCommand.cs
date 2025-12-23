using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Commands.Pages;

public sealed record DeletePageCommand(Guid Id) : ICommand
{
    public sealed class Handler : ICommandHandler<DeletePageCommand>
    {
        private readonly IPageRepository _repository;

        public Handler(IPageRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result> Handle(DeletePageCommand request, CancellationToken cancellationToken)
        {
            var page = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (page is null)
            {
                return Result.Failure("صفحه مورد نظر یافت نشد.");
            }

            await _repository.DeleteAsync(page, cancellationToken);

            return Result.Success();
        }
    }
}

