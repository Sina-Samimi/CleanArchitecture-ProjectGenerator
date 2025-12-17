using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Catalog;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Catalog;

public sealed record GetProductAttributesQuery(Guid ProductId) : IQuery<ProductAttributesDto>
{
    public sealed class Handler : IQueryHandler<GetProductAttributesQuery, ProductAttributesDto>
    {
        private readonly IProductRepository _productRepository;

        public Handler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<ProductAttributesDto>> Handle(
            GetProductAttributesQuery request,
            CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                return Result<ProductAttributesDto>.Failure("محصول مورد نظر یافت نشد.");
            }

            var attributes = product.Attributes
                .Where(attr => !attr.IsDeleted)
                .OrderBy(attr => attr.DisplayOrder)
                .ThenBy(attr => attr.CreateDate)
                .Select(attr => new ProductAttributeDto(
                    attr.Id,
                    attr.Key,
                    attr.Value,
                    attr.DisplayOrder))
                .ToArray();

            var dto = new ProductAttributesDto(product.Id, product.Name, attributes);
            return Result<ProductAttributesDto>.Success(dto);
        }
    }
}

