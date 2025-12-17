using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Blogs;
using TestAttarClone.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TestAttarClone.Infrastructure.Persistence.Repositories;

public sealed class BlogCommentRepository : IBlogCommentRepository
{
    private readonly AppDbContext _dbContext;

    public BlogCommentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<BlogComment>> GetByBlogIdAsync(Guid blogId, CancellationToken cancellationToken)
    {
        if (blogId == Guid.Empty)
        {
            return Array.Empty<BlogComment>();
        }

        return await _dbContext.BlogComments
            .AsNoTracking()
            .Include(comment => comment.ApprovedBy)
            .Where(comment => comment.BlogId == blogId && !comment.IsDeleted)
            .OrderBy(comment => comment.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public Task<BlogComment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Task.FromResult<BlogComment?>(null);
        }

        return _dbContext.BlogComments
            .Include(comment => comment.ApprovedBy)
            .FirstOrDefaultAsync(comment => comment.Id == id && !comment.IsDeleted, cancellationToken);
    }

    public async Task UpdateAsync(BlogComment comment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comment);

        _dbContext.BlogComments.Update(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
