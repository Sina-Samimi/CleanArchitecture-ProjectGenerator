using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Billing;
using Arsis.Domain.Entities.Billing;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken, bool includeDetails = false);

    Task<Invoice?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken, bool includeDetails = false);

    Task<Invoice?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Invoice>> GetListAsync(InvoiceListFilterDto? filter, CancellationToken cancellationToken);

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
}
