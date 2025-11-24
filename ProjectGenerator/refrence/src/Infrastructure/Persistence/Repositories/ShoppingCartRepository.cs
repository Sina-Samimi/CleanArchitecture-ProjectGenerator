using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class ShoppingCartRepository : IShoppingCartRepository
{
    private readonly AppDbContext _dbContext;

    public ShoppingCartRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShoppingCart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var normalizedUserId = userId.Trim();

        return await Query()
            .FirstOrDefaultAsync(cart => cart.UserId == normalizedUserId && !cart.IsDeleted, cancellationToken);
    }

    public async Task<ShoppingCart?> GetByAnonymousIdAsync(Guid anonymousId, CancellationToken cancellationToken)
    {
        if (anonymousId == Guid.Empty)
        {
            return null;
        }

        return await Query()
            .FirstOrDefaultAsync(cart => cart.AnonymousId == anonymousId && !cart.IsDeleted, cancellationToken);
    }

    public async Task<ShoppingCart?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await Query()
            .FirstOrDefaultAsync(cart => cart.Id == id && !cart.IsDeleted, cancellationToken);

    public async Task AddAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cart);

        await _dbContext.ShoppingCarts.AddAsync(cart, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cart);

        _dbContext.ShoppingCarts.Update(cart);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cart);

        _dbContext.ShoppingCarts.Remove(cart);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ShoppingCart> Query()
        => _dbContext.ShoppingCarts
            .Include(cart => cart.Items)
            .AsSplitQuery();
}
