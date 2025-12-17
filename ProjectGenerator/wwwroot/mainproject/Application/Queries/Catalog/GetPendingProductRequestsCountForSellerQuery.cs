using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.Domain.Enums;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Catalog;

public sealed record GetPendingProductRequestsCountForSellerQuery(string SellerId) : IQuery<int>;

public sealed class GetPendingProductRequestsCountForSellerQueryHandler : IQueryHandler<GetPendingProductRequestsCountForSellerQuery, int>
{
    private readonly IProductRequestRepository _productRequestRepository;

    public GetPendingProductRequestsCountForSellerQueryHandler(IProductRequestRepository productRequestRepository)
    {
        _productRequestRepository = productRequestRepository;
    }

    public async Task<Result<int>> Handle(GetPendingProductRequestsCountForSellerQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SellerId))
        {
            return Result<int>.Failure("شناسه فروشنده معتبر نیست.");
        }

        var requests = await _productRequestRepository.GetBySellerIdAsync(request.SellerId.Trim(), cancellationToken);
        var pendingCount = requests.Count(r => !r.IsDeleted && r.Status == ProductRequestStatus.Pending);
        
        return Result<int>.Success(pendingCount);
    }
}

