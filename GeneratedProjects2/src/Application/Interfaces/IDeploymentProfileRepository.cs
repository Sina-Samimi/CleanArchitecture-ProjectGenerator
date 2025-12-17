using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Domain.Entities.Settings;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface IDeploymentProfileRepository
{
    Task<IReadOnlyCollection<DeploymentProfile>> GetAllAsync(CancellationToken cancellationToken);

    Task<DeploymentProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<DeploymentProfile?> GetByBranchAsync(string branch, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken);

    Task<bool> ExistsByBranchAsync(string branch, Guid? excludeId, CancellationToken cancellationToken);

    Task AddAsync(DeploymentProfile profile, CancellationToken cancellationToken);

    Task UpdateAsync(DeploymentProfile profile, CancellationToken cancellationToken);

    Task DeleteAsync(DeploymentProfile profile, CancellationToken cancellationToken);
}
