using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Domain.Entities.Orders;

namespace Arsis.Application.Interfaces;

public interface IShoppingCartRepository
{
    Task<ShoppingCart?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<ShoppingCart?> GetByAnonymousIdAsync(Guid anonymousId, CancellationToken cancellationToken);

    Task<ShoppingCart?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(ShoppingCart cart, CancellationToken cancellationToken);

    Task UpdateAsync(ShoppingCart cart, CancellationToken cancellationToken);

    Task RemoveAsync(ShoppingCart cart, CancellationToken cancellationToken);
}
