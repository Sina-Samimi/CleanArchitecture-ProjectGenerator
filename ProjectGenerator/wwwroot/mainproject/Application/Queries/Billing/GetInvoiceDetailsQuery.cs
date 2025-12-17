using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Billing;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Billing;

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
