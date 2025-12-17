using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Domain.Entities.Orders;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface IShipmentTrackingRepository
{
    Task<ShipmentTracking?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ShipmentTracking?> GetByInvoiceItemIdAsync(Guid invoiceItemId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ShipmentTracking>> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ShipmentTracking>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ShipmentTracking>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task AddAsync(ShipmentTracking tracking, CancellationToken cancellationToken);

    Task UpdateAsync(ShipmentTracking tracking, CancellationToken cancellationToken);
}

