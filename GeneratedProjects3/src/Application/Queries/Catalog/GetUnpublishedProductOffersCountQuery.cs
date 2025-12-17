using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Catalog;

public sealed record GetUnpublishedProductOffersCountQuery() : IQuery<int>;

public sealed class GetUnpublishedProductOffersCountQueryHandler : IQueryHandler<GetUnpublishedProductOffersCountQuery, int>
{
    private readonly IProductOfferRepository _productOfferRepository;

    public GetUnpublishedProductOffersCountQueryHandler(IProductOfferRepository productOfferRepository)
    {
        _productOfferRepository = productOfferRepository;
    }

    public async Task<Result<int>> Handle(GetUnpublishedProductOffersCountQuery request, CancellationToken cancellationToken)
    {
        var count = await _productOfferRepository.GetUnpublishedActiveCountAsync(cancellationToken);
        return Result<int>.Success(count);
    }
}

