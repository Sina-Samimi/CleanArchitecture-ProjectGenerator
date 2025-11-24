using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Catalog;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Catalog;

public sealed record GetProductDetailQuery(Guid Id) : IQuery<ProductDetailDto>
{
    public sealed class Handler : IQueryHandler<GetProductDetailQuery, ProductDetailDto>
    {
        private readonly IProductRepository _productRepository;

        public Handler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<ProductDetailDto>> Handle(GetProductDetailQuery request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetWithDetailsAsync(request.Id, cancellationToken);
            if (product is null || product.IsDeleted)
            {
                return Result<ProductDetailDto>.Failure("محصول مورد نظر یافت نشد.");
            }

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
                product.SeoTitle,
                product.SeoDescription,
                product.SeoKeywords,
                product.SeoSlug,
                product.Robots,
                product.TagList ?? string.Empty,
                product.FeaturedImagePath,
                product.DigitalDownloadPath,
                product.TeacherId,
                product.Gallery
                    .OrderBy(image => image.DisplayOrder)
                    .ThenBy(image => image.CreateDate)
                    .Select(image => new ProductGalleryImageDto(image.Id, image.ImagePath, image.DisplayOrder))
                    .ToArray());

            return Result<ProductDetailDto>.Success(dto);
        }
    }
}
