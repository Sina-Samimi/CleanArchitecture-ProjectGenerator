using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Billing;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Exceptions;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Billing;

public sealed record ApproveWithdrawalRequestCommand(
    Guid RequestId,
    string? AdminNotes) : ICommand<WithdrawalRequestDetailsDto>;

public sealed class ApproveWithdrawalRequestCommandHandler : ICommandHandler<ApproveWithdrawalRequestCommand, WithdrawalRequestDetailsDto>
{
    private readonly IWithdrawalRequestRepository _withdrawalRequestRepository;

    public ApproveWithdrawalRequestCommandHandler(IWithdrawalRequestRepository withdrawalRequestRepository)
    {
        _withdrawalRequestRepository = withdrawalRequestRepository;
    }

    public async Task<Result<WithdrawalRequestDetailsDto>> Handle(ApproveWithdrawalRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var withdrawalRequest = await _withdrawalRequestRepository.GetByIdForUpdateAsync(request.RequestId, cancellationToken);
            if (withdrawalRequest is null)
            {
                return Result<WithdrawalRequestDetailsDto>.Failure("درخواست برداشت یافت نشد.");
            }

            withdrawalRequest.Approve(request.AdminNotes);

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

