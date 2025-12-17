using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Catalog;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Catalog;

public sealed record GetProductDetailQuery(Guid Id) : IQuery<ProductDetailDto>
{
    public sealed class Handler : IQueryHandler<GetProductDetailQuery, ProductDetailDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly IVisitRepository _visitRepository;

        public Handler(IProductRepository productRepository, IVisitRepository visitRepository)
        {
            _productRepository = productRepository;
            _visitRepository = visitRepository;
        }

        public async Task<Result<ProductDetailDto>> Handle(GetProductDetailQuery request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetWithDetailsAsync(request.Id, cancellationToken);
            if (product is null || product.IsDeleted)
            {
                return Result<ProductDetailDto>.Failure("محصول مورد نظر یافت نشد.");
            }

            var viewCount = await _visitRepository.GetProductVisitCountAsync(request.Id, cancellationToken);

            var variantAttributes = product.VariantAttributes
                .OrderBy(attr => attr.DisplayOrder)
                .ThenBy(attr => attr.Name)
                .Select(attr => new ProductVariantAttributeDto(
                    attr.Id,
                    attr.Name,
                    attr.Options,
                    attr.DisplayOrder))
                .ToArray();

            var variants = product.Variants
                .Where(v => v.IsActive)
                .Select(v => new ProductVariantDto(
                    v.Id,
                    v.Price,
                    v.CompareAtPrice,
                    v.StockQuantity,
                    v.Sku,
                    v.ImagePath,
                    v.IsActive,
                    v.Options
                        .Select(opt => new ProductVariantOptionDto(
                            opt.Id,
                            opt.VariantAttributeId,
                            opt.Value))
                        .ToArray()))
                .ToArray();

            var dto = new ProductDetailDto(
                product.Id,
                product.Name,
                product.Summary,
                product.Description,
                product.Type,
                product.Price,
                product.CompareAtPrice,
                product.TrackInventory,
                product.StockQuantity,
                product.IsPublished,
                product.PublishedAt,
                product.CategoryId,
                product.Category.Name,
                product.Brand,
                product.SeoTitle,
                product.SeoDescription,
                product.SeoKeywords,
                product.SeoSlug,
                product.Robots,
                product.TagList ?? string.Empty,
                product.FeaturedImagePath,
                product.DigitalDownloadPath,
                product.SellerId,
                product.Gallery
                    .OrderBy(image => image.DisplayOrder)
                    .ThenBy(image => image.CreateDate)
                    .Select(image => new ProductGalleryImageDto(image.Id, image.ImagePath, image.DisplayOrder))
                    .ToArray(),
                viewCount,
                product.IsCustomOrder,
                variantAttributes,
                variants);

            return Result<ProductDetailDto>.Success(dto);
        }
    }
}
