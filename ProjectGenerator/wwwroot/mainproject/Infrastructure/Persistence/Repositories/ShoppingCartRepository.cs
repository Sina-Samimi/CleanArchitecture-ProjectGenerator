using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Attar.Infrastructure.Persistence.Repositories;

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

        var entry = _dbContext.Entry(cart);
        
        if (entry.State == EntityState.Detached)
        {
            // Check if cart with same ID is already tracked
            var trackedCart = _dbContext.ChangeTracker.Entries<ShoppingCart>()
                .FirstOrDefault(e => e.Entity.Id == cart.Id);
            
            if (trackedCart is not null && !ReferenceEquals(trackedCart.Entity, cart))
            {
                // Detach the tracked entity
                trackedCart.State = EntityState.Detached;
                
                // Detach all tracked items from the old cart
                var trackedItems = _dbContext.ChangeTracker.Entries<ShoppingCartItem>()
                    .Where(e => e.Entity.CartId == cart.Id)
                    .ToList();
                
                foreach (var itemEntry in trackedItems)
                {
                    itemEntry.State = EntityState.Detached;
                }
            }
            
            // Reload entity from database to get current UpdateDate and existing items
            var existingCart = await _dbContext.ShoppingCarts
                .Include(c => c.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == cart.Id, cancellationToken);
            
            if (existingCart is not null)
            {
                // Set the original UpdateDate to match database before Update()
                entry = _dbContext.Entry(cart);
                entry.Property(nameof(cart.UpdateDate)).OriginalValue = existingCart.UpdateDate;
                
                // Track existing items and mark them appropriately
                var existingItemIds = existingCart.Items.Select(i => i.Id).ToHashSet();
                
                // First attach the cart
                _dbContext.ShoppingCarts.Attach(cart);
                
                // Then handle items
                foreach (var item in cart.Items)
                {
                    var itemEntry = _dbContext.Entry(item);
                    
                    if (item.Id == Guid.Empty || !existingItemIds.Contains(item.Id))
                    {
                        // New item - mark as Added
                        if (itemEntry.State == EntityState.Detached)
                        {
                            itemEntry.State = EntityState.Added;
                        }
                    }
                    else
                    {
                        // Existing item - attach and mark as Modified
                        if (itemEntry.State == EntityState.Detached)
                        {
                            _dbContext.ShoppingCartItems.Attach(item);
                        }
                        itemEntry.State = EntityState.Modified;
                    }
                }
            }
            else
            {
                // Cart doesn't exist in database - this shouldn't happen for Update
                _dbContext.ShoppingCarts.Attach(cart);
            }
            
            entry.State = EntityState.Modified;
        }
        else
        {
            // Entity is already tracked, refresh UpdateDate original value before marking as modified
            try
            {
                // Reload UpdateDate and existing items from database
                var existingCart = await _dbContext.ShoppingCarts
                    .Include(c => c.Items)
                    .AsNoTracking()
                    .Where(c => c.Id == cart.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (existingCart is not null)
                {
                    entry.Property(nameof(cart.UpdateDate)).OriginalValue = existingCart.UpdateDate;
                    
                    // Ensure all items are properly tracked
                    var existingItemIds = existingCart.Items.Select(i => i.Id).ToHashSet();
                    
                    foreach (var item in cart.Items)
                    {
                        var itemEntry = _dbContext.Entry(item);
                        
                        if (itemEntry.State == EntityState.Detached)
                        {
                            if (item.Id == Guid.Empty || !existingItemIds.Contains(item.Id))
                            {
                                // New item - add it explicitly
                                _dbContext.ShoppingCartItems.Add(item);
                            }
                            else
                            {
                                // Existing item - attach it
                                _dbContext.ShoppingCartItems.Attach(item);
                                itemEntry.State = EntityState.Modified;
                            }
                        }
                        else if (item.Id != Guid.Empty && existingItemIds.Contains(item.Id))
                        {
                            // Existing item that's already tracked - ensure it's marked as modified
                            if (itemEntry.State == EntityState.Unchanged)
                            {
                                itemEntry.State = EntityState.Modified;
                            }
                        }
                        else if (item.Id == Guid.Empty || !existingItemIds.Contains(item.Id))
                        {
                            // New item that's already tracked - ensure it's marked as Added
                            if (itemEntry.State != EntityState.Added)
                            {
                                itemEntry.State = EntityState.Added;
                            }
                        }
                    }
                }
            }
            catch
            {
                // If we can't get database values, continue anyway
            }
            
            // Mark cart as modified
            entry.State = EntityState.Modified;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cart);

        _dbContext.ShoppingCarts.Remove(cart);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<ShoppingCart> Items, int TotalCount)> GetActiveCartsAsync(
        string? userId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ShoppingCarts
            .Where(cart => !cart.IsDeleted && cart.Items.Any())
            .AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var normalizedUserId = userId.Trim();
            query = query.Where(cart => cart.UserId == normalizedUserId);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(cart => cart.UpdateDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(cart => cart.UpdateDate <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(cart => cart.Items)
            .OrderByDescending(cart => cart.UpdateDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private IQueryable<ShoppingCart> Query()
        => _dbContext.ShoppingCarts
            .Include(cart => cart.Items)
            .AsSplitQuery()
            .AsTracking();
}
