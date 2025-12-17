using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Billing;

public sealed record GetPendingWithdrawalRequestsCountQuery() : IQuery<int>;

public sealed class GetPendingWithdrawalRequestsCountQueryHandler : IQueryHandler<GetPendingWithdrawalRequestsCountQuery, int>
{
    private readonly IWithdrawalRequestRepository _withdrawalRequestRepository;

    public GetPendingWithdrawalRequestsCountQueryHandler(IWithdrawalRequestRepository withdrawalRequestRepository)
    {
        _withdrawalRequestRepository = withdrawalRequestRepository;
    }

    public async Task<Result<int>> Handle(GetPendingWithdrawalRequestsCountQuery request, CancellationToken cancellationToken)
    {
        var requests = await _withdrawalRequestRepository.GetAllAsync(WithdrawalRequestStatus.Pending, null, cancellationToken);
        var count = requests.Count;

        return Result<int>.Success(count);
    }
}

