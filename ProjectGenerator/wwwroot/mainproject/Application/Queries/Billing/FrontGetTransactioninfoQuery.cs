using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Billing;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Queries.Billing;

using System;

public sealed record FrontGetTransactioninfoQuery(Guid InvoiceId) : IQuery<FrontTransactionInfoDto>;

public sealed class FrontGetTransactioninfoQueryHandler : IQueryHandler<FrontGetTransactioninfoQuery, FrontTransactionInfoDto>
{
    private readonly IInvoiceRepository _invoiceRepository;

    public FrontGetTransactioninfoQueryHandler(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<FrontTransactionInfoDto>> Handle(FrontGetTransactioninfoQuery request, CancellationToken cancellationToken)
    {
        if (request.InvoiceId == Guid.Empty)
        {
            return Result<FrontTransactionInfoDto>.Failure("شناسه فاکتور نامعتبر است.");
        }

        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken, includeDetails: false);
        if (invoice is null)
        {
            return Result<FrontTransactionInfoDto>.Failure("فاکتور مورد نظر پیدا نشد.");
        }

        var dto = new FrontTransactionInfoDto(
            invoice.GrandTotal,
            invoice.ShippingRecipientPhone ?? string.Empty,
            invoice.UserId ?? string.Empty,
            invoice.Id);

        return Result<FrontTransactionInfoDto>.Success(dto);
    }
}
