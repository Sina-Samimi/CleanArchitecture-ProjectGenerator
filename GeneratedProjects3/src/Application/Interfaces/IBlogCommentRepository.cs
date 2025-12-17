using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Domain.Entities.Blogs;

namespace LogTableRenameTest.Application.Interfaces;

public interface IBlogCommentRepository
{
    Task<IReadOnlyCollection<BlogComment>> GetByBlogIdAsync(Guid blogId, CancellationToken cancellationToken);

    Task<BlogComment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(BlogComment comment, CancellationToken cancellationToken);
}
