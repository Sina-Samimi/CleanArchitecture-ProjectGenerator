using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.Domain.Enums;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Catalog;

public sealed record UpdateProductCustomRequestStatusCommand(
    Guid RequestId,
    CustomRequestStatus Status,
    string? AdminNotes) : ICommand
{
    public sealed class Handler : ICommandHandler<UpdateProductCustomRequestStatusCommand>
    {
        private readonly IProductCustomRequestRepository _requestRepository;

        public Handler(IProductCustomRequestRepository requestRepository)
        {
            _requestRepository = requestRepository;
        }

        public async Task<Result> Handle(UpdateProductCustomRequestStatusCommand request, CancellationToken cancellationToken)
        {
            if (request.RequestId == Guid.Empty)
            {
                return Result.Failure("شناسه درخواست معتبر نیست.");
            }

            var customRequest = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
            if (customRequest is null || customRequest.IsDeleted)
            {
                return Result.Failure("درخواست مورد نظر یافت نشد.");
            }

            customRequest.UpdateStatus(request.Status);

            if (!string.IsNullOrWhiteSpace(request.AdminNotes))
            {
                customRequest.UpdateAdminNotes(request.AdminNotes);
            }

            await _requestRepository.UpdateAsync(customRequest, cancellationToken);

            return Result.Success();
        }
    }
}

