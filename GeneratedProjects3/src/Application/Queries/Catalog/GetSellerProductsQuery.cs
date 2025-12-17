using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Catalog;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Catalog;

public sealed record GetSellerProductsQuery(string UserId) : IQuery<IReadOnlyCollection<ProductListItemDto>>
{
    public sealed class Handler : IQueryHandler<GetSellerProductsQuery, IReadOnlyCollection<ProductListItemDto>>
    {
        private readonly IProductRepository _productRepository;

        public Handler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<IReadOnlyCollection<ProductListItemDto>>> Handle(
            GetSellerProductsQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<IReadOnlyCollection<ProductListItemDto>>.Failure("شناسه کاربر برای دریافت محصولات معتبر نیست.");
            }

            var items = await _productRepository.GetBySellerAsync(request.UserId, cancellationToken);

            return Result<IReadOnlyCollection<ProductListItemDto>>.Success(items);
        }
    }
}
