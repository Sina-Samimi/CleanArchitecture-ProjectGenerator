using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Attar.Domain.Entities.Catalog;
using Attar.Domain.Enums;

namespace Attar.Application.Interfaces;

public interface ISiteCategoryRepository
{
    Task<SiteCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(SiteCategory category, CancellationToken cancellationToken);

    Task UpdateAsync(SiteCategory category, CancellationToken cancellationToken);

    Task RemoveAsync(SiteCategory category, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SiteCategory>> GetTreeAsync(CategoryScope scope, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SiteCategory>> GetByScopeAsync(CategoryScope scope, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SiteCategory>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Guid>> GetDescendantIdsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsBySlugAsync(string slug, CategoryScope scope, Guid? excludeId, CancellationToken cancellationToken);
}
