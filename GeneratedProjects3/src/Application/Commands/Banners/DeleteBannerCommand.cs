using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Banners;

public sealed record DeleteBannerCommand(Guid Id) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteBannerCommand>
    {
        private readonly IBannerRepository _repository;

        public Handler(IBannerRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result> Handle(DeleteBannerCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
            {
                return Result.Failure("شناسه بنر معتبر نیست.");
            }

            var banner = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken);
            if (banner is null)
            {
                return Result.Failure("بنر یافت نشد.");
            }

            await _repository.DeleteAsync(banner, cancellationToken);

            return Result.Success();
        }
    }
}

