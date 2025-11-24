using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Blogs;
using Arsis.Domain.Entities.Blogs;

namespace Arsis.Application.Interfaces;

public interface IBlogRepository
{
    Task<Blog?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Blog?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Blog blog, CancellationToken cancellationToken);

    Task UpdateAsync(Blog blog, CancellationToken cancellationToken);

    Task RemoveAsync(Blog blog, CancellationToken cancellationToken);

    Task<BlogListResultDto> GetListAsync(BlogListFilterDto filter, IReadOnlyCollection<Guid>? categoryIds, CancellationToken cancellationToken);

    Task<bool> ExistsInCategoriesAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken);

    Task<bool> ExistsByAuthorAsync(Guid authorId, CancellationToken cancellationToken);
}
