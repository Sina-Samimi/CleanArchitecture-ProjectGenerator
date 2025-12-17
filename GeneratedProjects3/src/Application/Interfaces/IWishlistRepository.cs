using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Domain.Entities.Catalog;

namespace LogTableRenameTest.Application.Interfaces;

public interface IWishlistRepository
{
    Task<Wishlist?> GetByUserAndProductAsync(string userId, Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Wishlist>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<bool> IsInWishlistAsync(string userId, Guid productId, CancellationToken cancellationToken);

    Task AddAsync(Wishlist wishlist, CancellationToken cancellationToken);

    Task RemoveAsync(Wishlist wishlist, CancellationToken cancellationToken);
}

