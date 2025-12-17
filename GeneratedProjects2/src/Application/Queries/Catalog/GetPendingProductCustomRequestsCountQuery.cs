using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Catalog;

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

