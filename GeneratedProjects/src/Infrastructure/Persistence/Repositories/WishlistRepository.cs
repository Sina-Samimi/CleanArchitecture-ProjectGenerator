using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace TestAttarClone.Infrastructure.Persistence.Repositories;

public sealed class WishlistRepository : IWishlistRepository
{
    private readonly AppDbContext _dbContext;

    public WishlistRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Wishlist?> GetByUserAndProductAsync(string userId, Guid productId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId) || productId == Guid.Empty)
        {
            return null;
        }

        var normalizedUserId = userId.Trim();

        return await Query()
            .FirstOrDefaultAsync(w => w.UserId == normalizedUserId && w.ProductId == productId && !w.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Wishlist>> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<Wishlist>();
        }

        var normalizedUserId = userId.Trim();

        var wishlists = await Query()
            .Where(w => w.UserId == normalizedUserId && !w.IsDeleted)
            .ToListAsync(cancellationToken);

        return wishlists;
    }

    public async Task<bool> IsInWishlistAsync(string userId, Guid productId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId) || productId == Guid.Empty)
        {
            return false;
        }

        var normalizedUserId = userId.Trim();

        return await _dbContext.Wishlists
            .AnyAsync(w => w.UserId == normalizedUserId && w.ProductId == productId && !w.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(Wishlist wishlist, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wishlist);

        await _dbContext.Wishlists.AddAsync(wishlist, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Wishlist wishlist, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wishlist);

        _dbContext.Wishlists.Remove(wishlist);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Wishlist> Query()
        => _dbContext.Wishlists
            .Include(w => w.Product)
                .ThenInclude(p => p.Category)
            .Include(w => w.Product)
                .ThenInclude(p => p.Comments.Where(c => !c.IsDeleted && c.IsApproved))
            .AsTracking();
}

