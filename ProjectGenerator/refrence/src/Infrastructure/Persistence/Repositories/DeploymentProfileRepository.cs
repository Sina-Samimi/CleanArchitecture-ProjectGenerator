using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class DeploymentProfileRepository : IDeploymentProfileRepository
{
    private readonly AppDbContext _dbContext;

    public DeploymentProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<DeploymentProfile>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.DeploymentProfiles
            .AsNoTracking()
            .OrderBy(profile => profile.Name)
            .ToArrayAsync(cancellationToken);

    public async Task<DeploymentProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.DeploymentProfiles
            .FirstOrDefaultAsync(profile => profile.Id == id, cancellationToken);

    public async Task<DeploymentProfile?> GetByBranchAsync(string branch, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(branch);

        var normalizedBranch = branch.Trim();

        return await _dbContext.DeploymentProfiles
            .FirstOrDefaultAsync(profile => profile.Branch == normalizedBranch, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(name);

        var normalizedName = name.Trim();

        return await _dbContext.DeploymentProfiles
            .AnyAsync(profile => profile.Name == normalizedName && (excludeId == null || profile.Id != excludeId), cancellationToken);
    }

    public async Task<bool> ExistsByBranchAsync(string branch, Guid? excludeId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(branch);

        var normalizedBranch = branch.Trim();

        return await _dbContext.DeploymentProfiles
            .AnyAsync(profile => profile.Branch == normalizedBranch && (excludeId == null || profile.Id != excludeId), cancellationToken);
    }

    public async Task AddAsync(DeploymentProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await _dbContext.DeploymentProfiles.AddAsync(profile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DeploymentProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _dbContext.DeploymentProfiles.Update(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(DeploymentProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _dbContext.DeploymentProfiles.Remove(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
