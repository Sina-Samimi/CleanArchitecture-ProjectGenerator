using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Domain.Entities.Sellers;

namespace TestAttarClone.Application.Interfaces;

public interface ISellerProfileRepository
{
    Task<IReadOnlyCollection<SellerProfile>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SellerProfile>> GetActiveAsync(CancellationToken cancellationToken);

    Task<SellerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<SellerProfile?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<SellerProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<bool> ExistsByUserIdAsync(string userId, Guid? excludeId, CancellationToken cancellationToken);

    Task AddAsync(SellerProfile seller, CancellationToken cancellationToken);

    Task UpdateAsync(SellerProfile seller, CancellationToken cancellationToken);
}
