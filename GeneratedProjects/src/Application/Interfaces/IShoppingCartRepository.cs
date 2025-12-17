using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Domain.Entities.Orders;

namespace TestAttarClone.Application.Interfaces;

public interface IShoppingCartRepository
{
    Task<ShoppingCart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<ShoppingCart?> GetByAnonymousIdAsync(Guid anonymousId, CancellationToken cancellationToken);

    Task<ShoppingCart?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(ShoppingCart cart, CancellationToken cancellationToken);

    Task UpdateAsync(ShoppingCart cart, CancellationToken cancellationToken);

    Task RemoveAsync(ShoppingCart cart, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ShoppingCart> Items, int TotalCount)> GetActiveCartsAsync(
        string? userId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int skip,
        int take,
        CancellationToken cancellationToken);
}
