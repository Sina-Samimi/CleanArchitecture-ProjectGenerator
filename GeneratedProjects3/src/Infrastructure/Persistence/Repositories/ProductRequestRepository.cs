using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities.Catalog;
using LogTableRenameTest.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogTableRenameTest.Infrastructure.Persistence.Repositories;

public sealed class ProductRequestRepository : IProductRequestRepository
{
    private readonly AppDbContext _dbContext;

    public ProductRequestRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.ProductRequests
            .Include(r => r.Category)
            .Include(r => r.TargetProduct)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    public async Task<ProductRequest?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.ProductRequests
            .Include(r => r.Category)
            .Include(r => r.Gallery)
            .Include(r => r.ApprovedProduct)
            .Include(r => r.TargetProduct)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(ProductRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        AttachCategory(request);
        await _dbContext.ProductRequests.AddAsync(request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProductRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        AttachCategory(request);
        _dbContext.ProductRequests.Update(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductRequest>> GetBySellerIdAsync(string sellerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
        {
            return Array.Empty<ProductRequest>();
        }

        return await _dbContext.ProductRequests
            .AsNoTracking()
            .Include(r => r.Category)
            .Where(r => !r.IsDeleted && r.SellerId == sellerId)
            .OrderByDescending(r => r.CreateDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductRequest>> GetByStatusAsync(ProductRequestStatus status, CancellationToken cancellationToken)
    {
        return await _dbContext.ProductRequests
            .AsNoTracking()
            .Include(r => r.Category)
            .Where(r => !r.IsDeleted && r.Status == status)
            .OrderByDescending(r => r.CreateDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductRequest>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        return await _dbContext.ProductRequests
            .AsNoTracking()
            .Include(r => r.Category)
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreateDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ProductRequests
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetCountByStatusAsync(ProductRequestStatus status, CancellationToken cancellationToken)
    {
        return await _dbContext.ProductRequests
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == status)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        var normalizedSlug = slug.Trim();

        var query = _dbContext.ProductRequests
            .AsNoTracking()
            .Where(request => !request.IsDeleted && request.SeoSlug == normalizedSlug);

        if (excludeId.HasValue)
        {
            var excluded = excludeId.Value;
            query = query.Where(request => request.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductRequest>> GetByTargetProductIdAsync(Guid targetProductId, CancellationToken cancellationToken)
    {
        if (targetProductId == Guid.Empty)
        {
            return Array.Empty<ProductRequest>();
        }

        return await _dbContext.ProductRequests
            .AsNoTracking()
            .Include(r => r.Category)
            .Where(r => !r.IsDeleted 
                && r.TargetProductId == targetProductId 
                && r.Status == Domain.Enums.ProductRequestStatus.Approved)
            .OrderBy(r => r.Price ?? decimal.MaxValue)
            .ThenByDescending(r => r.CreateDate)
            .ToArrayAsync(cancellationToken);
    }

    private void AttachCategory(ProductRequest request)
    {
        if (request.Category is null)
        {
            return;
        }

        // Check if category is already tracked in the change tracker
        var trackedCategory = _dbContext.ChangeTracker
            .Entries<SiteCategory>()
            .FirstOrDefault(entry => entry.Entity.Id == request.Category.Id);

        if (trackedCategory is null)
        {
            // Category is not tracked, attach it as unchanged to prevent EF from trying to insert it
            // This tells EF that the category already exists in the database
            _dbContext.Attach(request.Category);
        }
        // If category is already tracked, no action needed - EF will use the tracked entity
    }
}

