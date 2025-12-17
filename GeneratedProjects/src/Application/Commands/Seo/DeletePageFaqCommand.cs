using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Seo;

public sealed record DeletePageFaqCommand(Guid Id) : ICommand
{
    public sealed class Handler : ICommandHandler<DeletePageFaqCommand>
    {
        private readonly IPageFaqRepository _repository;

        public Handler(IPageFaqRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result> Handle(DeletePageFaqCommand request, CancellationToken cancellationToken)
        {
            var pageFaq = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken);

            if (pageFaq is null)
            {
                return Result.Failure("سوال متداول مورد نظر یافت نشد.");
            }

            await _repository.DeleteAsync(pageFaq, cancellationToken);

            return Result.Success();
        }
    }
}

