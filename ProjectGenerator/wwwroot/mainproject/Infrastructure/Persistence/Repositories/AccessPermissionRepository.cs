using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Interfaces;
using Attar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attar.Infrastructure.Persistence.Repositories;

public sealed class AccessPermissionRepository : IAccessPermissionRepository
{
    private readonly AppDbContext _dbContext;

    public AccessPermissionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccessPermission?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.AccessPermissions.FirstOrDefaultAsync(permission => permission.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<AccessPermission>> GetCustomAsync(CancellationToken cancellationToken)
        => await _dbContext.AccessPermissions
            .AsNoTracking()
            .OrderBy(permission => permission.GroupKey)
            .ThenBy(permission => permission.DisplayName)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsWithDisplayNameAsync(string displayName, Guid? excludeId, CancellationToken cancellationToken)
    {
        var query = _dbContext.AccessPermissions.AsNoTracking().Where(permission => permission.DisplayName == displayName);

        if (excludeId.HasValue)
        {
            query = query.Where(permission => permission.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(AccessPermission permission, CancellationToken cancellationToken)
        => await _dbContext.AccessPermissions.AddAsync(permission, cancellationToken);

    public void Remove(AccessPermission permission)
        => _dbContext.AccessPermissions.Remove(permission);

    public void RemoveRange(IEnumerable<AccessPermission> permissions)
        => _dbContext.AccessPermissions.RemoveRange(permissions);

    public async Task<IReadOnlyCollection<AccessPermission>> GetByKeyPrefixAsync(string keyPrefix, CancellationToken cancellationToken)
        => await _dbContext.AccessPermissions
            .Where(permission => permission.Key.StartsWith(keyPrefix))
            .ToListAsync(cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
