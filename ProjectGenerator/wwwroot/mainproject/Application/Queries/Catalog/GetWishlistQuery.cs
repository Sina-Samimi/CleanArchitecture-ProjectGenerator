using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Catalog;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Catalog;

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

