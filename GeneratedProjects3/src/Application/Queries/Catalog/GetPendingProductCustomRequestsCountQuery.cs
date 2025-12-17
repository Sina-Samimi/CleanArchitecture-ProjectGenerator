using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Catalog;

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

