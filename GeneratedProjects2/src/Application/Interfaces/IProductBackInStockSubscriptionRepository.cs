using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Domain.Entities.Catalog;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface IProductBackInStockSubscriptionRepository
{
    Task<ProductBackInStockSubscription?> GetActiveAsync(Guid? productId, Guid? productOfferId, string phoneNumber, CancellationToken cancellationToken);

    Task AddAsync(ProductBackInStockSubscription subscription, CancellationToken cancellationToken);

    Task<ProductBackInStockSubscription?> GetForUserAsync(Guid productId, string userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductBackInStockSubscription>> GetPendingNotificationsForProductAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductBackInStockSubscription>> GetPendingNotificationsForOfferAsync(Guid offerId, CancellationToken cancellationToken);

    Task UpdateAsync(ProductBackInStockSubscription subscription, CancellationToken cancellationToken);
}


