using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Domain.Entities.Blogs;

namespace TestAttarClone.Application.Interfaces;

public interface IBlogAuthorRepository
{
    Task<IReadOnlyCollection<BlogAuthor>> GetAllAsync(CancellationToken cancellationToken);

    Task<BlogAuthor?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<BlogAuthor?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(BlogAuthor author, CancellationToken cancellationToken);

    Task UpdateAsync(BlogAuthor author, CancellationToken cancellationToken);

    Task RemoveAsync(BlogAuthor author, CancellationToken cancellationToken);

    Task<bool> ExistsByDisplayNameAsync(string displayName, Guid? excludeId, CancellationToken cancellationToken);

    Task<bool> ExistsByUserAsync(string userId, Guid? excludeId, CancellationToken cancellationToken);
}
