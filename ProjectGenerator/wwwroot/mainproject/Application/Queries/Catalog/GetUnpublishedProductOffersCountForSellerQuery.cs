using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Catalog;

public sealed record GetUnpublishedProductOffersCountForSellerQuery(string SellerId) : IQuery<int>;

public sealed class GetUnpublishedProductOffersCountForSellerQueryHandler : IQueryHandler<GetUnpublishedProductOffersCountForSellerQuery, int>
{
    private readonly IProductOfferRepository _productOfferRepository;

    public GetUnpublishedProductOffersCountForSellerQueryHandler(IProductOfferRepository productOfferRepository)
    {
        _productOfferRepository = productOfferRepository;
    }

    public async Task<Result<int>> Handle(GetUnpublishedProductOffersCountForSellerQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SellerId))
        {
            return Result<int>.Failure("شناسه فروشنده معتبر نیست.");
        }

        var offers = await _productOfferRepository.GetBySellerIdAsync(request.SellerId.Trim(), includeInactive: true, cancellationToken);
        var unpublishedCount = offers.Count(o => !o.IsDeleted && o.IsActive && !o.IsPublished);
        
        return Result<int>.Success(unpublishedCount);
    }
}

