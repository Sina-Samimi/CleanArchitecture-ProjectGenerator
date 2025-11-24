using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Catalog;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Catalog;

public sealed record GetTeacherProductsQuery(string UserId) : IQuery<IReadOnlyCollection<ProductListItemDto>>
{
    public sealed class Handler : IQueryHandler<GetTeacherProductsQuery, IReadOnlyCollection<ProductListItemDto>>
    {
        private readonly IProductRepository _productRepository;

        public Handler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<IReadOnlyCollection<ProductListItemDto>>> Handle(
            GetTeacherProductsQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<IReadOnlyCollection<ProductListItemDto>>.Failure("شناسه کاربر برای دریافت محصولات معتبر نیست.");
            }

            var items = await _productRepository.GetByTeacherAsync(request.UserId, cancellationToken);

            return Result<IReadOnlyCollection<ProductListItemDto>>.Success(items);
        }
    }
}
