using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Pages;

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

