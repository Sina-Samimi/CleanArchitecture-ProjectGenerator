using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Domain.Entities;

namespace MobiRooz.Application.Interfaces;

public interface IAccessPermissionRepository
{
    Task<AccessPermission?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AccessPermission>> GetCustomAsync(CancellationToken cancellationToken);

    Task<bool> ExistsWithDisplayNameAsync(string displayName, Guid? excludeId, CancellationToken cancellationToken);

    Task AddAsync(AccessPermission permission, CancellationToken cancellationToken);

    void Remove(AccessPermission permission);

    void RemoveRange(IEnumerable<AccessPermission> permissions);

    Task<IReadOnlyCollection<AccessPermission>> GetByKeyPrefixAsync(string keyPrefix, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
