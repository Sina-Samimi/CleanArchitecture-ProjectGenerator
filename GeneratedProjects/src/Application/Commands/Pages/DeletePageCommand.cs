using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Pages;

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

