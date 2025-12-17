using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Catalog;

public sealed record GetPendingProductCustomRequestsCountQuery() : IQuery<int>;

public sealed class GetPendingProductCustomRequestsCountQueryHandler : IQueryHandler<GetPendingProductCustomRequestsCountQuery, int>
{
    private readonly IProductCustomRequestRepository _productCustomRequestRepository;

    public GetPendingProductCustomRequestsCountQueryHandler(IProductCustomRequestRepository productCustomRequestRepository)
    {
        _productCustomRequestRepository = productCustomRequestRepository;
    }

    public async Task<Result<int>> Handle(GetPendingProductCustomRequestsCountQuery request, CancellationToken cancellationToken)
    {
        var count = await _productCustomRequestRepository.GetCountByStatusAsync(CustomRequestStatus.Pending, cancellationToken);
        return Result<int>.Success(count);
    }
}

