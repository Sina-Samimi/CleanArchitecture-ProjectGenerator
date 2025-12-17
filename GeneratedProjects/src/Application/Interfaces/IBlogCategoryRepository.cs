using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Domain.Entities.Blogs;

namespace TestAttarClone.Application.Interfaces;

public interface IBlogCategoryRepository
{
    Task<IReadOnlyCollection<BlogCategory>> GetAllAsync(CancellationToken cancellationToken);

    Task<BlogCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<BlogCategory?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(BlogCategory category, CancellationToken cancellationToken);

    Task UpdateAsync(BlogCategory category, CancellationToken cancellationToken);

    Task RemoveAsync(BlogCategory category, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Guid>> GetDescendantIdsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken);
}
