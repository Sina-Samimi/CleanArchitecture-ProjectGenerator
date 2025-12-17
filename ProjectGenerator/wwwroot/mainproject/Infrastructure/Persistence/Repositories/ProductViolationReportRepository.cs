using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Catalog;
using Attar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attar.Infrastructure.Persistence.Repositories;

public sealed class ProductViolationReportRepository : IProductViolationReportRepository
{
    private readonly AppDbContext _dbContext;

    public ProductViolationReportRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductViolationReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.ProductViolationReports
            .Include(report => report.Product)
            .Include(report => report.ProductOffer)
            .FirstOrDefaultAsync(report => report.Id == id && !report.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductViolationReport>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return Array.Empty<ProductViolationReport>();
        }

        return await _dbContext.ProductViolationReports
            .Include(report => report.Product)
            .Include(report => report.ProductOffer)
            .Where(report => report.ProductId == productId && !report.IsDeleted)
            .OrderByDescending(report => report.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductViolationReport>> GetBySellerIdAsync(string sellerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
        {
            return Array.Empty<ProductViolationReport>();
        }

        return await _dbContext.ProductViolationReports
            .Include(report => report.Product)
            .Include(report => report.ProductOffer)
            .Where(report => report.SellerId == sellerId && !report.IsDeleted)
            .OrderByDescending(report => report.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductViolationReport>> GetByProductOfferIdAsync(Guid productOfferId, CancellationToken cancellationToken)
    {
        if (productOfferId == Guid.Empty)
        {
            return Array.Empty<ProductViolationReport>();
        }

        return await _dbContext.ProductViolationReports
            .Include(report => report.Product)
            .Include(report => report.ProductOffer)
            .Where(report => report.ProductOfferId == productOfferId && !report.IsDeleted)
            .OrderByDescending(report => report.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductViolationReport>> GetAllAsync(CancellationToken cancellationToken, bool? isReviewed = null)
    {
        var query = _dbContext.ProductViolationReports
            .Include(report => report.Product)
            .Include(report => report.ProductOffer)
            .Where(report => !report.IsDeleted)
            .AsQueryable();

        if (isReviewed.HasValue)
        {
            query = query.Where(report => report.IsReviewed == isReviewed.Value);
        }

        return await query
            .OrderByDescending(report => report.CreateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return 0;
        }

        return await _dbContext.ProductViolationReports
            .CountAsync(report => report.ProductId == productId && !report.IsDeleted, cancellationToken);
    }

    public async Task<int> GetCountBySellerIdAsync(string sellerId, CancellationToken cancellationToken, bool? isReviewed = null)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
        {
            return 0;
        }

        var query = _dbContext.ProductViolationReports
            .Where(report => report.SellerId == sellerId && !report.IsDeleted)
            .AsQueryable();

        if (isReviewed.HasValue)
        {
            query = query.Where(report => report.IsReviewed == isReviewed.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> GetUnreviewedCountAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ProductViolationReports
            .CountAsync(report => !report.IsReviewed && !report.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(ProductViolationReport report, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);

        _dbContext.ProductViolationReports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProductViolationReport report, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);

        _dbContext.ProductViolationReports.Update(report);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

