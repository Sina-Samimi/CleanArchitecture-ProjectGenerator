using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Catalog;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Catalog;

public sealed record GetTeacherProductDetailQuery(Guid ProductId, string TeacherId)
    : IQuery<TeacherProductDetailDto>
{
    public sealed class Handler : IQueryHandler<GetTeacherProductDetailQuery, TeacherProductDetailDto>
    {
        private readonly IProductRepository _productRepository;

        public Handler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<TeacherProductDetailDto>> Handle(
            GetTeacherProductDetailQuery request,
            CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result<TeacherProductDetailDto>.Failure("شناسه محصول نامعتبر است.");
            }

            if (string.IsNullOrWhiteSpace(request.TeacherId))
            {
                return Result<TeacherProductDetailDto>.Failure("دسترسی به محصول امکان‌پذیر نیست.");
            }

            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null || product.IsDeleted)
            {
                return Result<TeacherProductDetailDto>.Failure("محصول مورد نظر یافت نشد.");
            }

            var isOwner = string.Equals(product.CreatorId, request.TeacherId, StringComparison.Ordinal)
                || string.Equals(product.TeacherId, request.TeacherId, StringComparison.Ordinal);

            if (!isOwner)
            {
                return Result<TeacherProductDetailDto>.Failure("شما اجازه ویرایش این محصول را ندارید.");
            }

            var gallery = product.Gallery
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.CreateDate)
                .Select(image => new ProductGalleryImageDto(image.Id, image.ImagePath, image.DisplayOrder))
                .ToArray();

            var dto = new TeacherProductDetailDto(
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
                product.TagList ?? string.Empty,
                product.FeaturedImagePath,
                product.DigitalDownloadPath,
                product.IsPublished,
                product.PublishedAt,
                product.CreateDate,
                product.UpdateDate,
                gallery);

            return Result<TeacherProductDetailDto>.Success(dto);
        }
    }
}
