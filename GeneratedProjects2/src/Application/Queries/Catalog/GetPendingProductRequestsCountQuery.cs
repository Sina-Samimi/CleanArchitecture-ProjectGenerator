using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Catalog;

public sealed record GetPendingProductRequestsCountQuery() : IQuery<int>;

public sealed class GetPendingProductRequestsCountQueryHandler : IQueryHandler<GetPendingProductRequestsCountQuery, int>
{
    private readonly IProductRequestRepository _productRequestRepository;

    public GetPendingProductRequestsCountQueryHandler(IProductRequestRepository productRequestRepository)
    {
        _productRequestRepository = productRequestRepository;
    }

    public async Task<Result<int>> Handle(GetPendingProductRequestsCountQuery request, CancellationToken cancellationToken)
    {
        var count = await _productRequestRepository.GetCountByStatusAsync(ProductRequestStatus.Pending, cancellationToken);
        return Result<int>.Success(count);
    }
}

