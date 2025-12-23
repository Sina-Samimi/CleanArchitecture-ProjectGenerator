using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Catalog;
using MobiRooz.Application.Interfaces;
using MobiRooz.Application.Queries.Identity.GetUsersByIds;
using MobiRooz.Domain.Enums;
using MobiRooz.SharedKernel.BaseTypes;
using MediatR;

namespace MobiRooz.Application.Queries.Catalog;

public sealed record GetProductRequestDetailQuery(Guid ProductRequestId) : IQuery<ProductRequestDetailDto>;

public sealed record ProductRequestDetailDto(
    Guid Id,
    string Name,
    string Summary,
    string Description,
    ProductType Type,
    decimal? Price,
    bool TrackInventory,
    int StockQuantity,
    Guid CategoryId,
    string CategoryName,
    string? FeaturedImagePath,
    string? DigitalDownloadPath,
    string TagList,
    string SellerId,
    string? SellerName,
    string? SellerPhone,
    ProductRequestStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ReviewedAt,
    string? ReviewerId,
    string? ReviewerName,
    string? RejectionReason,
    Guid? ApprovedProductId,
    string? SeoTitle,
    string? SeoDescription,
    string? SeoKeywords,
    string SeoSlug,
    string? Robots,
    bool IsCustomOrder,
    IReadOnlyCollection<ProductRequestGalleryImageDto> Gallery);

public sealed record ProductRequestGalleryImageDto(
    Guid Id,
    string Path,
    int Order);

public sealed class GetProductRequestDetailQueryHandler : IQueryHandler<GetProductRequestDetailQuery, ProductRequestDetailDto>
{
    private readonly IProductRequestRepository _requestRepository;
    private readonly ISellerProfileRepository _sellerProfileRepository;
    private readonly IMediator _mediator;

    public GetProductRequestDetailQueryHandler(
        IProductRequestRepository requestRepository,
        ISellerProfileRepository sellerProfileRepository,
        IMediator mediator)
    {
        _requestRepository = requestRepository;
        _sellerProfileRepository = sellerProfileRepository;
        _mediator = mediator;
    }

    public async Task<Result<ProductRequestDetailDto>> Handle(
        GetProductRequestDetailQuery request,
        CancellationToken cancellationToken)
    {
        var productRequest = await _requestRepository.GetByIdWithDetailsAsync(
            request.ProductRequestId,
            cancellationToken);

        if (productRequest is null)
        {
            return Result<ProductRequestDetailDto>.Failure("درخواست محصول یافت نشد.");
        }

        if (productRequest.IsDeleted)
        {
            return Result<ProductRequestDetailDto>.Failure("درخواست محصول حذف شده است.");
        }

        var galleryDtos = productRequest.Gallery
            .OrderBy(img => img.Order)
            .Select(img => new ProductRequestGalleryImageDto(
                img.Id,
                img.Path,
                img.Order))
            .ToArray();

        // Get seller information
        string? sellerName = null;
        string? sellerPhone = null;
        if (!string.IsNullOrWhiteSpace(productRequest.SellerId))
        {
            var seller = await _sellerProfileRepository.GetByUserIdAsync(productRequest.SellerId, cancellationToken);
            if (seller is not null && !seller.IsDeleted)
            {
                sellerName = seller.DisplayName;
                sellerPhone = seller.ContactPhone;
            }
        }

        // Get reviewer information
        string? reviewerName = null;
        if (!string.IsNullOrWhiteSpace(productRequest.ReviewerId))
        {
            var userIds = new[] { productRequest.ReviewerId };
            var userLookupResult = await _mediator.Send(new GetUsersByIdsQuery(userIds), cancellationToken);
            if (userLookupResult.IsSuccess && userLookupResult.Value is not null && userLookupResult.Value.TryGetValue(productRequest.ReviewerId, out var reviewer))
            {
                reviewerName = reviewer.DisplayName;
            }
        }

        var dto = new ProductRequestDetailDto(
            productRequest.Id,
            productRequest.Name,
            productRequest.Summary,
            productRequest.Description,
            productRequest.Type,
            productRequest.Price,
            productRequest.TrackInventory,
            productRequest.StockQuantity,
            productRequest.CategoryId,
            productRequest.Category?.Name ?? "نامشخص",
            productRequest.FeaturedImagePath,
            productRequest.DigitalDownloadPath,
            productRequest.TagList,
            productRequest.SellerId,
            sellerName,
            sellerPhone,
            productRequest.Status,
            productRequest.CreateDate,
            productRequest.UpdateDate,
            productRequest.ReviewedAt,
            productRequest.ReviewerId,
            reviewerName,
            productRequest.RejectionReason,
            productRequest.ApprovedProductId,
            productRequest.SeoTitle,
            productRequest.SeoDescription,
            productRequest.SeoKeywords,
            productRequest.SeoSlug,
            productRequest.Robots,
            productRequest.IsCustomOrder,
            galleryDtos);

        return Result<ProductRequestDetailDto>.Success(dto);
    }
}

