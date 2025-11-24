using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class PageAccessPolicyRepository : IPageAccessPolicyRepository
{
    private readonly AppDbContext _dbContext;

    public PageAccessPolicyRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<PageAccessPolicy>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.PageAccessPolicies
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<PageAccessPolicy>> GetByPageAsync(
        string area,
        string controller,
        string action,
        CancellationToken cancellationToken)
        => await _dbContext.PageAccessPolicies
            .AsNoTracking()
            .Where(policy =>
                policy.Area == (area ?? string.Empty) &&
                policy.Controller == controller &&
                policy.Action == action)
            .ToListAsync(cancellationToken);

    public async Task ReplacePoliciesAsync(
        string area,
        string controller,
        string action,
        IReadOnlyCollection<string> permissionKeys,
        CancellationToken cancellationToken)
    {
        var normalizedArea = area ?? string.Empty;
        var normalizedController = controller.Trim();
        var normalizedAction = action.Trim();

        var existing = await _dbContext.PageAccessPolicies
            .Where(policy =>
                policy.Area == normalizedArea &&
                policy.Controller == normalizedController &&
                policy.Action == normalizedAction)
            .ToListAsync(cancellationToken);

        var requested = new HashSet<string>(permissionKeys ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var policy in existing)
        {
            if (!requested.Contains(policy.PermissionKey))
            {
                _dbContext.PageAccessPolicies.Remove(policy);
            }
        }

        foreach (var permission in requested)
        {
            var alreadyExists = existing.Any(policy =>
                string.Equals(policy.PermissionKey, permission, StringComparison.OrdinalIgnoreCase));

            if (!alreadyExists)
            {
                var newPolicy = new PageAccessPolicy(normalizedArea, normalizedController, normalizedAction, permission);
                await _dbContext.PageAccessPolicies.AddAsync(newPolicy, cancellationToken);
            }
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
