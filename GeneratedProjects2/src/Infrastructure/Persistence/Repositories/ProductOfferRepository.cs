using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Repositories;

public sealed class ProductOfferRepository : IProductOfferRepository
{
    private readonly AppDbContext _dbContext;

    public ProductOfferRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductOffer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.ProductOffers
            .Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);
    }

    public async Task<ProductOffer?> GetByProductIdAndSellerIdAsync(Guid productId, string sellerId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty || string.IsNullOrWhiteSpace(sellerId))
        {
            return null;
        }

        return await _dbContext.ProductOffers
            .Include(o => o.Product)
            .FirstOrDefaultAsync(
                o => o.ProductId == productId 
                    && o.SellerId == sellerId.Trim() 
                    && !o.IsDeleted, 
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductOffer>> GetByProductIdAsync(
        Guid productId, 
        bool includeInactive = false, 
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            return Array.Empty<ProductOffer>();
        }

        var query = _dbContext.ProductOffers
            .AsNoTracking()
            .Include(o => o.Product)
            .Where(o => o.ProductId == productId && !o.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(o => o.IsActive);
        }

        return await query
            .OrderByDescending(o => o.IsPublished)
            .ThenBy(o => o.Price ?? decimal.MaxValue)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductOffer>> GetBySellerIdAsync(
        string sellerId, 
        bool includeInactive = false, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
        {
            return Array.Empty<ProductOffer>();
        }

        var query = _dbContext.ProductOffers
            .AsNoTracking()
            .Include(o => o.Product)
            .Where(o => o.SellerId == sellerId.Trim() && !o.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(o => o.IsActive);
        }

        return await query
            .OrderByDescending(o => o.CreateDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductOffer>> GetBySellerIdForUpdateAsync(
        string sellerId, 
        bool includeInactive = false, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
        {
            return Array.Empty<ProductOffer>();
        }

        var query = _dbContext.ProductOffers
            .Include(o => o.Product)
            .Where(o => o.SellerId == sellerId.Trim() && !o.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(o => o.IsActive);
        }

        return await query
            .OrderByDescending(o => o.CreateDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductOffer>> GetActiveOffersByProductIdAsync(
        Guid productId, 
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return Array.Empty<ProductOffer>();
        }

        return await _dbContext.ProductOffers
            .AsNoTracking()
            .Include(o => o.Product)
            .Where(o => o.ProductId == productId 
                && !o.IsDeleted 
                && o.IsActive 
                && o.IsPublished)
            .OrderBy(o => o.Price ?? decimal.MaxValue)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductOffer>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = _dbContext.ProductOffers
            .AsNoTracking()
            .Include(o => o.Product)
            .Where(o => !o.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(o => o.IsActive);
        }

        return await query
            .OrderByDescending(o => o.CreateDate)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(ProductOffer offer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offer);

        AttachProduct(offer);
        await _dbContext.ProductOffers.AddAsync(offer, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProductOffer offer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offer);

        AttachProduct(offer);
        _dbContext.ProductOffers.Update(offer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateBatchAsync(IReadOnlyCollection<ProductOffer> offers, CancellationToken cancellationToken)
    {
        if (offers is null || offers.Count == 0)
        {
            return;
        }

        // Since entities are already tracked (from GetBySellerIdForUpdateAsync),
        // we just need to ensure Product is attached and save changes
        foreach (var offer in offers)
        {
            AttachProduct(offer);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid productId, string sellerId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty || string.IsNullOrWhiteSpace(sellerId))
        {
            return false;
        }

        return await _dbContext.ProductOffers
            .AsNoTracking()
            .AnyAsync(
                o => o.ProductId == productId 
                    && o.SellerId == sellerId.Trim() 
                    && !o.IsDeleted, 
                cancellationToken);
    }

    public async Task<int> GetUnpublishedActiveCountAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ProductOffers
            .AsNoTracking()
            .Where(o => !o.IsDeleted && o.IsActive && !o.IsPublished)
            .CountAsync(cancellationToken);
    }

    private void AttachProduct(ProductOffer offer)
    {
        if (offer.Product is null)
        {
            return;
        }

        // Check if product is already tracked in the change tracker
        var trackedProduct = _dbContext.ChangeTracker
            .Entries<Product>()
            .FirstOrDefault(entry => entry.Entity.Id == offer.Product.Id);

        if (trackedProduct is null)
        {
            // Product is not tracked, attach it as unchanged to prevent EF from trying to insert it
            _dbContext.Entry(offer.Product).State = EntityState.Unchanged;
        }
    }
}

