using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Catalog;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Catalog;

public sealed record GetSellerProductDetailQuery(Guid ProductId, string SellerId)
    : IQuery<SellerProductDetailDto>
{
    public sealed class Handler : IQueryHandler<GetSellerProductDetailQuery, SellerProductDetailDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IVisitRepository _visitRepository;

        public Handler(IProductRepository productRepository, IVisitRepository visitRepository)
        {
            _productRepository = productRepository;
            _visitRepository = visitRepository;
        }

        public async Task<Result<SellerProductDetailDto>> Handle(
            GetSellerProductDetailQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result<SellerProductDetailDto>.Failure("شناسه محصول نامعتبر است.");
            }

            if (string.IsNullOrWhiteSpace(request.SellerId))
            {
                return Result<SellerProductDetailDto>.Failure("دسترسی به محصول امکان‌پذیر نیست.");
            }

            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null || product.IsDeleted)
            {
                return Result<SellerProductDetailDto>.Failure("محصول مورد نظر یافت نشد.");
            }

            var isOwner = string.Equals(product.CreatorId, request.SellerId, StringComparison.Ordinal)
                || string.Equals(product.SellerId, request.SellerId, StringComparison.Ordinal);

            if (!isOwner)
            {
                return Result<SellerProductDetailDto>.Failure("شما اجازه ویرایش این محصول را ندارید.");
            }

            var gallery = product.Gallery
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.CreateDate)
                .Select(image => new ProductGalleryImageDto(image.Id, image.ImagePath, image.DisplayOrder))
                .ToArray();

            var viewCount = await _visitRepository.GetProductVisitCountAsync(request.ProductId, cancellationToken);

            var dto = new SellerProductDetailDto(
                product.Id,
                product.Name,
                product.Summary,
                product.Description,
                product.Type,
                product.Price,
                product.CompareAtPrice,
                product.TrackInventory,
                product.StockQuantity,
                product.CategoryId,
                product.Category.Name,
                product.Brand,
                product.TagList ?? string.Empty,
                product.IsCustomOrder,
                product.FeaturedImagePath,
                product.DigitalDownloadPath,
                product.IsPublished,
                product.PublishedAt,
                product.CreateDate,
                product.UpdateDate,
                gallery,
                viewCount);

            return Result<SellerProductDetailDto>.Success(dto);
        }
    }
}
