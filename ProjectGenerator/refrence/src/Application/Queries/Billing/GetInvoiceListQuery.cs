using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Billing;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Billing;

public sealed record GetInvoiceListQuery(InvoiceListFilterDto? Filter) : IQuery<InvoiceListResultDto>;

public sealed class GetInvoiceListQueryHandler : IQueryHandler<GetInvoiceListQuery, InvoiceListResultDto>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoiceListQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<InvoiceListResultDto>> Handle(GetInvoiceListQuery request, CancellationToken cancellationToken)
    {
        var invoices = await _invoiceRepository.GetListAsync(request.Filter, cancellationToken);
        var items = invoices.Select(invoice => invoice.ToListItemDto()).ToArray();
        var summary = invoices.BuildSummary();
        var result = new InvoiceListResultDto(items, summary, DateTimeOffset.UtcNow);
        return Result<InvoiceListResultDto>.Success(result);
    }
}
