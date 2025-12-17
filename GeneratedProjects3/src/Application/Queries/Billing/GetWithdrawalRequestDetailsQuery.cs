using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Billing;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Billing;

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

