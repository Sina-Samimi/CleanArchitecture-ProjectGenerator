using System;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Billing;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Billing;

public sealed record GetWithdrawalRequestDetailsQuery(Guid RequestId) : IQuery<WithdrawalRequestDetailsDto>;

public sealed class GetWithdrawalRequestDetailsQueryHandler : IQueryHandler<GetWithdrawalRequestDetailsQuery, WithdrawalRequestDetailsDto>
{
    private readonly IWithdrawalRequestRepository _withdrawalRequestRepository;

    public GetWithdrawalRequestDetailsQueryHandler(IWithdrawalRequestRepository withdrawalRequestRepository)
    {
        _withdrawalRequestRepository = withdrawalRequestRepository;
    }

    public async Task<Result<WithdrawalRequestDetailsDto>> Handle(GetWithdrawalRequestDetailsQuery request, CancellationToken cancellationToken)
    {
        var withdrawalRequest = await _withdrawalRequestRepository.GetByIdAsync(request.RequestId, cancellationToken);

        if (withdrawalRequest is null)
        {
            return Result<WithdrawalRequestDetailsDto>.Failure("درخواست برداشت یافت نشد.");
        }

        return Result<WithdrawalRequestDetailsDto>.Success(withdrawalRequest.ToDetailsDto());
    }
}

