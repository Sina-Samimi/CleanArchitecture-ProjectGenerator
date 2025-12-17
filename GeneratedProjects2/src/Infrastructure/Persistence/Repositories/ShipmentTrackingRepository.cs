using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Orders;
using LogsDtoCloneTest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Repositories;

public sealed class ShipmentTrackingRepository : IShipmentTrackingRepository
{
    private readonly AppDbContext _dbContext;

    public ShipmentTrackingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShipmentTracking?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.ShipmentTrackings
            .Include(tracking => tracking.InvoiceItem)
                .ThenInclude(item => item.Invoice)
            .Include(tracking => tracking.UpdatedBy)
            .FirstOrDefaultAsync(tracking => tracking.Id == id && !tracking.IsDeleted, cancellationToken);

    public async Task<ShipmentTracking?> GetByInvoiceItemIdAsync(Guid invoiceItemId, CancellationToken cancellationToken)
        => await _dbContext.ShipmentTrackings
            .Include(tracking => tracking.InvoiceItem)
                .ThenInclude(item => item.Invoice)
            .Include(tracking => tracking.UpdatedBy)
            .OrderByDescending(tracking => tracking.StatusDate)
            .FirstOrDefaultAsync(tracking => tracking.InvoiceItemId == invoiceItemId && !tracking.IsDeleted, cancellationToken);

    public async Task<IReadOnlyCollection<ShipmentTracking>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken)
        => await _dbContext.ShipmentTrackings
            .Include(tracking => tracking.InvoiceItem)
                .ThenInclude(item => item.Invoice)
            .Include(tracking => tracking.UpdatedBy)
            .Where(tracking => tracking.InvoiceItem.InvoiceId == invoiceId && !tracking.IsDeleted)
            .OrderByDescending(tracking => tracking.StatusDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ShipmentTracking>> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<ShipmentTracking>();
        }

        return await _dbContext.ShipmentTrackings
            .Include(tracking => tracking.InvoiceItem)
                .ThenInclude(item => item.Invoice)
            .Include(tracking => tracking.UpdatedBy)
            .Where(tracking => 
                tracking.InvoiceItem.Invoice.UserId == userId && 
                !tracking.IsDeleted &&
                !tracking.InvoiceItem.IsDeleted &&
                !tracking.InvoiceItem.Invoice.IsDeleted)
            .OrderByDescending(tracking => tracking.StatusDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ShipmentTracking>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            return Array.Empty<ShipmentTracking>();
        }

        return await _dbContext.ShipmentTrackings
            .Include(tracking => tracking.InvoiceItem)
                .ThenInclude(item => item.Invoice)
            .Include(tracking => tracking.UpdatedBy)
            .Where(tracking => 
                tracking.InvoiceItem.ReferenceId == productId && 
                !tracking.IsDeleted &&
                !tracking.InvoiceItem.IsDeleted &&
                !tracking.InvoiceItem.Invoice.IsDeleted)
            .OrderByDescending(tracking => tracking.StatusDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ShipmentTracking tracking, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tracking);
        await _dbContext.ShipmentTrackings.AddAsync(tracking, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ShipmentTracking tracking, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tracking);
        _dbContext.ShipmentTrackings.Update(tracking);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

