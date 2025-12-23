using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.DTOs.Billing;
using MobiRooz.Domain.Entities.Billing;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken, bool includeDetails = false);

    Task<Invoice?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken, bool includeDetails = false);

    Task<Invoice?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Invoice>> GetListAsync(InvoiceListFilterDto? filter, CancellationToken cancellationToken);
    
    Task<int> GetListCountAsync(InvoiceListFilterDto? filter, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Invoice>> GetListByUserAsync(string userId, int? take, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<InvoiceItem>> GetProductInvoiceItemsAsync(Guid productId, CancellationToken cancellationToken);

    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);

    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken);

    Task<bool> ExistsByNumberAsync(string invoiceNumber, Guid? excludeId, CancellationToken cancellationToken);

    Task<Result<TResult>> MutateAsync<TResult>(
        Guid invoiceId,
        bool includeDetails,
        Func<Invoice, CancellationToken, Task<Result<TResult>>> mutation,
        CancellationToken cancellationToken,
        string? notFoundMessage = null);

    Task<Invoice?> GetByTrackingHashAsync(int trackingHash, CancellationToken cancellationToken);

    Task<Invoice?> GetByTrackingNumberAsync(long trackingNumber, CancellationToken cancellationToken);
}
