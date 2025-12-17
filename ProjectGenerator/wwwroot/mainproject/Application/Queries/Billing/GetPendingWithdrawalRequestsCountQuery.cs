using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.Domain.Enums;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Billing;

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

