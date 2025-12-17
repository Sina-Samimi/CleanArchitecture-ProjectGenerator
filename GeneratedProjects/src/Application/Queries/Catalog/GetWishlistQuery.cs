using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Catalog;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Catalog;

public sealed record GetWishlistQuery(string UserId) : IQuery<IReadOnlyCollection<Product>>
{
    public sealed class Handler : IQueryHandler<GetWishlistQuery, IReadOnlyCollection<Product>>
    {
        private readonly IWishlistRepository _wishlistRepository;

        public Handler(IWishlistRepository wishlistRepository)
        {
            _wishlistRepository = wishlistRepository;
        }

        public async Task<Result<IReadOnlyCollection<Product>>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<IReadOnlyCollection<Product>>.Failure("شناسه کاربر معتبر نیست.");
            }

            var normalizedUserId = request.UserId.Trim();
            var wishlists = await _wishlistRepository.GetByUserIdAsync(normalizedUserId, cancellationToken);

            var products = wishlists
                .Where(w => w.Product != null && !w.Product.IsDeleted && w.Product.IsPublished)
                .Select(w => w.Product)
                .ToList();

            return Result<IReadOnlyCollection<Product>>.Success(products);
        }
    }
}

