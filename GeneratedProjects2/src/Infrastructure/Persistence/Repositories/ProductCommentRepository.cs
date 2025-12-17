using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Catalog;
using LogsDtoCloneTest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Repositories;

public sealed class ProductCommentRepository : IProductCommentRepository
{
    private readonly AppDbContext _dbContext;

    public ProductCommentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ProductComment>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return Array.Empty<ProductComment>();
        }

        return await _dbContext.ProductComments
            .Include(comment => comment.ApprovedBy)
            .Where(comment => comment.ProductId == productId && !comment.IsDeleted)
            .OrderBy(comment => comment.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductComment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.ProductComments
            .Include(comment => comment.ApprovedBy)
            .FirstOrDefaultAsync(comment => comment.Id == id && !comment.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductComment>> GetPendingAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ProductComments
            .AsNoTracking()
            .Include(c => c.Product)
            .Where(c => !c.IsDeleted && !c.IsApproved)
            .OrderByDescending(c => c.CreateDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(ProductComment comment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comment);

        _dbContext.ProductComments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProductComment comment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comment);

        _dbContext.ProductComments.Update(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
