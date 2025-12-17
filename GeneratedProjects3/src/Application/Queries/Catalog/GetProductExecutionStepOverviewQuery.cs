using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Catalog;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Catalog;

public sealed record GetProductExecutionStepOverviewQuery : IQuery<ProductExecutionStepsOverviewDto>
{
    public sealed class Handler : IQueryHandler<GetProductExecutionStepOverviewQuery, ProductExecutionStepsOverviewDto>
    {
        private readonly IProductRepository _productRepository;

        public Handler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<ProductExecutionStepsOverviewDto>> Handle(
            GetProductExecutionStepOverviewQuery request,
            CancellationToken cancellationToken)
        {
            var summaries = await _productRepository.GetExecutionStepSummariesAsync(cancellationToken);

            if (summaries.Count == 0)
            {
                var empty = new ProductExecutionStepsOverviewDto(
                    0,
                    0,
                    0,
                    0,
                    Array.Empty<ProductExecutionStepSummaryDto>());

                return Result<ProductExecutionStepsOverviewDto>.Success(empty);
            }

            var totalProducts = summaries.Count;
            var productsWithSteps = summaries.Count(item => item.StepCount > 0);
            var totalSteps = summaries.Sum(item => item.StepCount);
            var averageSteps = totalProducts == 0
                ? 0
                : Math.Round((double)totalSteps / totalProducts, 1, MidpointRounding.AwayFromZero);

            var dto = new ProductExecutionStepsOverviewDto(
                totalProducts,
                productsWithSteps,
                totalSteps,
                averageSteps,
                summaries);

            return Result<ProductExecutionStepsOverviewDto>.Success(dto);
        }
    }
}
