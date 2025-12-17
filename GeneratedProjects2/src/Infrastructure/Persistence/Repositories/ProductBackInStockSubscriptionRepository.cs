using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Repositories;

public sealed class ProductBackInStockSubscriptionRepository : IProductBackInStockSubscriptionRepository
{
    private readonly AppDbContext _dbContext;

    public ProductBackInStockSubscriptionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductBackInStockSubscription?> GetActiveAsync(Guid? productId, Guid? productOfferId, string phoneNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var normalizedPhone = phoneNumber.Trim();

        var query = _dbContext.ProductBackInStockSubscriptions
            .AsTracking()
            .Where(x => !x.IsDeleted && !x.IsNotified && x.PhoneNumber == normalizedPhone);

        if (productOfferId.HasValue && productOfferId.Value != Guid.Empty)
        {
            query = query.Where(x => x.ProductOfferId == productOfferId);
        }
        else if (productId.HasValue && productId.Value != Guid.Empty)
        {
            query = query.Where(x => x.ProductId == productId && x.ProductOfferId == null);
        }
        else
        {
            return null;
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(ProductBackInStockSubscription subscription, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        await _dbContext.ProductBackInStockSubscriptions.AddAsync(subscription, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductBackInStockSubscription>> GetPendingNotificationsForProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return Array.Empty<ProductBackInStockSubscription>();
        }

        return await _dbContext.ProductBackInStockSubscriptions
            .Where(x => x.ProductId == productId && x.ProductOfferId == null && !x.IsDeleted && !x.IsNotified)
            .AsTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductBackInStockSubscription?> GetForUserAsync(Guid productId, string userId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty || string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var normalizedUserId = userId.Trim();

        return await _dbContext.ProductBackInStockSubscriptions
            .AsTracking()
            .FirstOrDefaultAsync(
                x => x.ProductId == productId
                     && x.ProductOfferId == null
                     && x.UserId == normalizedUserId
                     && !x.IsDeleted
                     && !x.IsNotified,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductBackInStockSubscription>> GetPendingNotificationsForOfferAsync(Guid offerId, CancellationToken cancellationToken)
    {
        if (offerId == Guid.Empty)
        {
            return Array.Empty<ProductBackInStockSubscription>();
        }

        return await _dbContext.ProductBackInStockSubscriptions
            .Where(x => x.ProductOfferId == offerId && !x.IsDeleted && !x.IsNotified)
            .AsTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProductBackInStockSubscription subscription, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        _dbContext.ProductBackInStockSubscriptions.Update(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


