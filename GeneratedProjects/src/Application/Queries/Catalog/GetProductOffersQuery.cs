using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Catalog;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Catalog;

public sealed record GetProductOffersQuery(
    Guid? ProductId = null,
    string? SellerId = null,
    string? ProductName = null,
    bool IncludeInactive = false,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<ProductOfferListResultDto>;

public sealed record ProductOfferListResultDto(
    IReadOnlyCollection<ProductOfferDto> Offers,
    int TotalCount,
    int PageNumber,
    int PageSize);

public sealed record ProductOfferDto(
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

public sealed class GetProductOffersQueryHandler : IQueryHandler<GetProductOffersQuery, ProductOfferListResultDto>
{
    private readonly IProductOfferRepository _offerRepository;
    private readonly IProductRepository _productRepository;
    private readonly ISellerProfileRepository _sellerProfileRepository;

    public GetProductOffersQueryHandler(
        IProductOfferRepository offerRepository,
        IProductRepository productRepository,
        ISellerProfileRepository sellerProfileRepository)
    {
        _offerRepository = offerRepository;
        _productRepository = productRepository;
        _sellerProfileRepository = sellerProfileRepository;
    }

    public async Task<Result<ProductOfferListResultDto>> Handle(
        GetProductOffersQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        IReadOnlyCollection<Domain.Entities.Catalog.ProductOffer> offers;

        if (request.ProductId.HasValue && request.ProductId.Value != Guid.Empty)
        {
            offers = await _offerRepository.GetByProductIdAsync(
                request.ProductId.Value, 
                request.IncludeInactive, 
                cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.SellerId))
        {
            offers = await _offerRepository.GetBySellerIdAsync(
                request.SellerId.Trim(), 
                request.IncludeInactive, 
                cancellationToken);
        }
        else
        {
            // Get all offers
            offers = await _offerRepository.GetAllAsync(request.IncludeInactive, cancellationToken);
        }

        // Optional filter by product name (contains)
        IReadOnlyCollection<Domain.Entities.Catalog.ProductOffer> filteredOffers = offers;
        if (!string.IsNullOrWhiteSpace(request.ProductName))
        {
            var searchTerm = request.ProductName.Trim();
            var allProductIds = offers.Select(o => o.ProductId).Distinct().ToList();
            var allProducts = await _productRepository.GetByIdsAsync(allProductIds, cancellationToken);
            var matchedIds = allProducts
                .Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Id)
                .ToHashSet();

            filteredOffers = offers
                .Where(o => matchedIds.Contains(o.ProductId))
                .ToArray();
        }

        // Get seller IDs first to filter out site products
        var allSellerIds = filteredOffers
            .Where(o => !string.IsNullOrWhiteSpace(o.SellerId))
            .Select(o => o.SellerId!)
            .Distinct()
            .ToList();
        
        var sellerProfiles = await Task.WhenAll(
            allSellerIds.Select(async id => 
            {
                var profile = await _sellerProfileRepository.GetByUserIdAsync(id, cancellationToken);
                return (id, profile);
            }));
        var sellerMap = sellerProfiles
            .Where(s => s.profile is not null)
            .ToDictionary(s => s.id, s => s.profile!);

        // Filter out offers where seller doesn't exist in SellerProfiles (site products)
        var validOffers = filteredOffers
            .Where(offer => !string.IsNullOrWhiteSpace(offer.SellerId) && sellerMap.ContainsKey(offer.SellerId))
            .ToList();

        var totalCount = validOffers.Count;

        // Apply pagination
        var pagedOffers = validOffers
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Get product details
        var productIds = pagedOffers.Select(o => o.ProductId).Distinct().ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productMap = products.ToDictionary(p => p.Id);

        var dtos = pagedOffers.Select(offer =>
        {
            var product = productMap.TryGetValue(offer.ProductId, out var p) ? p : null;
            var seller = sellerMap.TryGetValue(offer.SellerId, out var s) ? s : null;

            return new ProductOfferDto(
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
        }).ToArray();

        var result = new ProductOfferListResultDto(
            dtos,
            totalCount,
            pageNumber,
            pageSize);

        return Result<ProductOfferListResultDto>.Success(result);
    }
}

