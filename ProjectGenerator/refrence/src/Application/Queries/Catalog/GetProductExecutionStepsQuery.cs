using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Catalog;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Catalog;

public sealed record GetProductExecutionStepsQuery(Guid ProductId) : IQuery<ProductExecutionStepsDto>
{
    public sealed class Handler : IQueryHandler<GetProductExecutionStepsQuery, ProductExecutionStepsDto>
    {
        private readonly IProductRepository _productRepository;

        public Handler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<ProductExecutionStepsDto>> Handle(
            GetProductExecutionStepsQuery request,
            CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null)
            {
                return Result<ProductExecutionStepsDto>.Failure("محصول مورد نظر یافت نشد.");
            }

            var steps = product.ExecutionSteps
                .Where(step => !step.IsDeleted)
                .OrderBy(step => step.DisplayOrder)
                .ThenBy(step => step.CreateDate)
                .Select(step => new ProductExecutionStepDto(
                    step.Id,
                    step.Title,
                    step.Description,
                    step.Duration,
                    step.DisplayOrder))
                .ToArray();

            var dto = new ProductExecutionStepsDto(product.Id, product.Name, steps);
            return Result<ProductExecutionStepsDto>.Success(dto);
        }
    }
}
