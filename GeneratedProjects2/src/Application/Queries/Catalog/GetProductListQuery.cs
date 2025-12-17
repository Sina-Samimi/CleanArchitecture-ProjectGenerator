using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Catalog;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Catalog;

public sealed record GetProductListQuery(
    string? Search,
    Guid? CategoryId,
    ProductType? Type,
    bool? IsPublished,
    decimal? MinPrice,
    decimal? MaxPrice,
    int Page,
    int PageSize,
    string? SellerId) : IQuery<ProductListResultDto>
{
    public sealed class Handler : IQueryHandler<GetProductListQuery, ProductListResultDto>
    {
        private readonly IProductRepository _productRepository;
        private readonly ISiteCategoryRepository _categoryRepository;

        public Handler(IProductRepository productRepository, ISiteCategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<ProductListResultDto>> Handle(GetProductListQuery request, CancellationToken cancellationToken)
        {
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 12 : Math.Clamp(request.PageSize, 5, 100);

            IReadOnlyCollection<Guid>? categoryScope = null;
            if (request.CategoryId.HasValue)
            {
                var descendantIds = await _categoryRepository.GetDescendantIdsAsync(request.CategoryId.Value, cancellationToken);
                if (descendantIds.Count == 0)
                {
                    categoryScope = new[] { request.CategoryId.Value };
                }
                else
                {
                    categoryScope = descendantIds.Contains(request.CategoryId.Value)
                        ? descendantIds
                        : descendantIds.Concat(new[] { request.CategoryId.Value }).ToArray();
                }
            }

            var filter = new ProductListFilterDto(
                page,
                pageSize,
                request.Search?.Trim(),
                request.Type,
                request.IsPublished,
                request.MinPrice,
                request.MaxPrice,
                request.SellerId?.Trim());

            var result = await _productRepository.GetListAsync(filter, categoryScope, cancellationToken);

            return Result<ProductListResultDto>.Success(result);
        }
    }
}
