using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Billing;
using Attar.Application.Interfaces;
using Attar.Domain.Enums;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Billing;

public sealed record GetUserInvoiceDetailsQuery(Guid Id, string UserId) : IQuery<InvoiceDetailDto>;

public sealed class GetUserInvoiceDetailsQueryHandler : IQueryHandler<GetUserInvoiceDetailsQuery, InvoiceDetailDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
        public GetUserInvoiceDetailsQueryHandler(
        IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
            }

    public async Task<Result<InvoiceDetailDto>> Handle(GetUserInvoiceDetailsQuery request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
        {
            return Result<InvoiceDetailDto>.Failure("شناسه فاکتور معتبر نیست.");
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<InvoiceDetailDto>.Failure("شناسه کاربر معتبر نیست.");
        }

        var invoice = await _invoiceRepository.GetByIdForUserAsync(request.Id, request.UserId, cancellationToken, includeDetails: true);
        if (invoice is null)
        {
            return Result<InvoiceDetailDto>.Failure("فاکتور مورد نظر یافت نشد یا دسترسی مجاز نیست.");
        }

        var detail = invoice.ToDetailDto();

                        // Test attempt handling removed
            return Result<InvoiceDetailDto>.Success(detail);
    }
}
