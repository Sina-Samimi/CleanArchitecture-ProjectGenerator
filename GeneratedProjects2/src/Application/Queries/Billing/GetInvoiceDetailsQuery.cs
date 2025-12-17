using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Billing;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Billing;

public sealed record GetInvoiceDetailsQuery(Guid Id) : IQuery<InvoiceDetailDto>;

public sealed class GetInvoiceDetailsQueryHandler : IQueryHandler<GetInvoiceDetailsQuery, InvoiceDetailDto>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoiceDetailsQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<InvoiceDetailDto>> Handle(GetInvoiceDetailsQuery request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
        {
            return Result<InvoiceDetailDto>.Failure("شناسه فاکتور معتبر نیست.");
        }

        var invoice = await _invoiceRepository.GetByIdAsync(request.Id, cancellationToken, includeDetails: true);
        if (invoice is null)
        {
            return Result<InvoiceDetailDto>.Failure("فاکتور مورد نظر یافت نشد.");
        }

        return Result<InvoiceDetailDto>.Success(invoice.ToDetailDto());
    }
}
