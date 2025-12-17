using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Catalog;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Catalog;

public sealed record GetProductFaqsQuery(Guid ProductId) : IQuery<ProductFaqsDto>
{
    public sealed class Handler : IQueryHandler<GetProductFaqsQuery, ProductFaqsDto>
    {
        private readonly IProductRepository _productRepository;

        public Handler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<ProductFaqsDto>> Handle(
            GetProductFaqsQuery request,
            CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                return Result<ProductFaqsDto>.Failure("محصول مورد نظر یافت نشد.");
            }

            var faqs = product.Faqs
                .Where(faq => !faq.IsDeleted)
                .OrderBy(faq => faq.DisplayOrder)
                .ThenBy(faq => faq.CreateDate)
                .Select(faq => new ProductFaqDto(
                    faq.Id,
                    faq.Question,
                    faq.Answer,
                    faq.DisplayOrder))
                .ToArray();

            var dto = new ProductFaqsDto(product.Id, product.Name, faqs);
            return Result<ProductFaqsDto>.Success(dto);
        }
    }
}
