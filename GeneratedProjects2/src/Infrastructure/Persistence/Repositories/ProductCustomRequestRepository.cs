using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Catalog;
using LogsDtoCloneTest.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Repositories;

public sealed class ProductCustomRequestRepository : IProductCustomRequestRepository
{
    private readonly AppDbContext _dbContext;

    public ProductCustomRequestRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductCustomRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.ProductCustomRequests
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(ProductCustomRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _dbContext.ProductCustomRequests.AddAsync(request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProductCustomRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _dbContext.ProductCustomRequests.Update(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductCustomRequest>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return Array.Empty<ProductCustomRequest>();
        }

        return await _dbContext.ProductCustomRequests
            .AsNoTracking()
            .Include(r => r.Product)
            .Where(r => !r.IsDeleted && r.ProductId == productId)
            .OrderByDescending(r => r.CreateDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductCustomRequest>> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<ProductCustomRequest>();
        }

        return await _dbContext.ProductCustomRequests
            .AsNoTracking()
            .Include(r => r.Product)
            .Where(r => !r.IsDeleted && r.UserId == userId)
            .OrderByDescending(r => r.CreateDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductCustomRequest>> GetByStatusAsync(CustomRequestStatus status, CancellationToken cancellationToken)
    {
        return await _dbContext.ProductCustomRequests
            .AsNoTracking()
            .Include(r => r.Product)
            .Where(r => !r.IsDeleted && r.Status == status)
            .OrderByDescending(r => r.CreateDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductCustomRequest>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        return await _dbContext.ProductCustomRequests
            .AsNoTracking()
            .Include(r => r.Product)
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreateDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ProductCustomRequests
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetCountByStatusAsync(CustomRequestStatus status, CancellationToken cancellationToken)
    {
        return await _dbContext.ProductCustomRequests
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == status)
            .CountAsync(cancellationToken);
    }
}

