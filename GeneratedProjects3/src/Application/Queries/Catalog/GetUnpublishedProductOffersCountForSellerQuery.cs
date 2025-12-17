using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Catalog;

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

