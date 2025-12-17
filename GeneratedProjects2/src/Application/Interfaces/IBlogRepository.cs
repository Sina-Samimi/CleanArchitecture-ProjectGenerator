using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.DTOs.Blogs;
using LogsDtoCloneTest.Domain.Entities.Blogs;

namespace LogsDtoCloneTest.Application.Interfaces;

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
