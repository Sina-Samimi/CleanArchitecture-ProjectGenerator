using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Billing;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Exceptions;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Billing;

public sealed record CancelWithdrawalRequestCommand(Guid RequestId) : ICommand<WithdrawalRequestDetailsDto>;

public sealed class CancelWithdrawalRequestCommandHandler : ICommandHandler<CancelWithdrawalRequestCommand, WithdrawalRequestDetailsDto>
{
    private readonly IWithdrawalRequestRepository _withdrawalRequestRepository;

    public CancelWithdrawalRequestCommandHandler(IWithdrawalRequestRepository withdrawalRequestRepository)
    {
        _withdrawalRequestRepository = withdrawalRequestRepository;
    }

    public async Task<Result<WithdrawalRequestDetailsDto>> Handle(CancelWithdrawalRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var withdrawalRequest = await _withdrawalRequestRepository.GetByIdForUpdateAsync(request.RequestId, cancellationToken);
            if (withdrawalRequest is null)
            {
                return Result<WithdrawalRequestDetailsDto>.Failure("درخواست برداشت یافت نشد.");
            }

            withdrawalRequest.Cancel();

            await _withdrawalRequestRepository.UpdateAsync(withdrawalRequest, cancellationToken);

            // Reload to get updated data
            withdrawalRequest = await _withdrawalRequestRepository.GetByIdAsync(request.RequestId, cancellationToken);

            return Result<WithdrawalRequestDetailsDto>.Success(withdrawalRequest!.ToDetailsDto());
        }
        catch (DomainException ex)
        {
            return Result<WithdrawalRequestDetailsDto>.Failure(ex.Message);
        }
    }
}

