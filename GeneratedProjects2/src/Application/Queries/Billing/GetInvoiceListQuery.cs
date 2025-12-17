using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Billing;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Billing;

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
        
        var pageNumber = request.Filter?.PageNumber ?? 1;
        var pageSize = request.Filter?.PageSize ?? 10;
        var totalCount = await _invoiceRepository.GetListCountAsync(request.Filter, cancellationToken);
        
        var result = new InvoiceListResultDto(
            items, 
            summary, 
            DateTimeOffset.UtcNow,
            pageNumber,
            pageSize,
            totalCount);
        return Result<InvoiceListResultDto>.Success(result);
    }
}
