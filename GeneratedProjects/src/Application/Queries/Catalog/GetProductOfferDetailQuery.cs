using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Catalog;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Catalog;

public sealed record GetProductOfferDetailQuery(Guid OfferId) : IQuery<ProductOfferDetailDto>;

public sealed record ProductOfferDetailDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductSlug,
    string SellerId,
    string? SellerName,
    string? SellerPhone,
    decimal? Price,
    decimal? CompareAtPrice,
    bool TrackInventory,
    int StockQuantity,
    bool IsActive,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid? ApprovedFromRequestId);

public sealed class GetProductOfferDetailQueryHandler : IQueryHandler<GetProductOfferDetailQuery, ProductOfferDetailDto>
{
    private readonly IProductOfferRepository _offerRepository;
    private readonly IProductRepository _productRepository;
    private readonly ISellerProfileRepository _sellerProfileRepository;

    public GetProductOfferDetailQueryHandler(
        IProductOfferRepository offerRepository,
        IProductRepository productRepository,
        ISellerProfileRepository sellerProfileRepository)
    {
        _offerRepository = offerRepository;
        _productRepository = productRepository;
        _sellerProfileRepository = sellerProfileRepository;
    }

    public async Task<Result<ProductOfferDetailDto>> Handle(
        GetProductOfferDetailQuery request,
        CancellationToken cancellationToken)
    {
        if (request.OfferId == Guid.Empty)
        {
            return Result<ProductOfferDetailDto>.Failure("شناسه پیشنهاد معتبر نیست.");
        }

        var offer = await _offerRepository.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.IsDeleted)
        {
            return Result<ProductOfferDetailDto>.Failure("پیشنهاد مورد نظر یافت نشد.");
        }

        var product = await _productRepository.GetByIdAsync(offer.ProductId, cancellationToken);
        var seller = await _sellerProfileRepository.GetByUserIdAsync(offer.SellerId, cancellationToken);

        var dto = new ProductOfferDetailDto(
            offer.Id,
            offer.ProductId,
            product?.Name ?? "نامشخص",
            product?.SeoSlug,
            offer.SellerId,
            seller?.DisplayName,
            seller?.ContactPhone,
            offer.Price,
            offer.CompareAtPrice,
            offer.TrackInventory,
            offer.StockQuantity,
            offer.IsActive,
            offer.IsPublished,
            offer.PublishedAt,
            offer.CreateDate,
            offer.UpdateDate,
            offer.ApprovedFromRequestId);

        return Result<ProductOfferDetailDto>.Success(dto);
    }
}

