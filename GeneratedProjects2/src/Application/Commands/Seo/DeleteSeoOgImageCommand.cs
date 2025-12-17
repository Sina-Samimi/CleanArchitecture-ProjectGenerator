using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Seo;

public sealed record DeleteSeoOgImageCommand(Guid Id) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteSeoOgImageCommand>
    {
        private readonly ISeoOgImageRepository _repository;

        public Handler(ISeoOgImageRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result> Handle(DeleteSeoOgImageCommand request, CancellationToken cancellationToken)
        {
            var image = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (image is null)
            {
                return Result.Failure("تصویر OG مورد نظر یافت نشد.");
            }

            await _repository.DeleteAsync(image, cancellationToken);

            return Result.Success();
        }
    }
}

