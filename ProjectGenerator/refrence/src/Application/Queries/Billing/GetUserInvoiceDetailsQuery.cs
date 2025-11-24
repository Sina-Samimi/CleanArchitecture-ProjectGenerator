using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Billing;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Billing;

public sealed record GetUserInvoiceDetailsQuery(Guid Id, string UserId) : IQuery<InvoiceDetailDto>;

public sealed class GetUserInvoiceDetailsQueryHandler : IQueryHandler<GetUserInvoiceDetailsQuery, InvoiceDetailDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUserTestAttemptRepository _attemptRepository;

    public GetUserInvoiceDetailsQueryHandler(
        IInvoiceRepository invoiceRepository,
        IUserTestAttemptRepository attemptRepository)
    {
        _invoiceRepository = invoiceRepository;
        _attemptRepository = attemptRepository;
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

        var testItem = detail.Items.FirstOrDefault(i => i.ItemType == InvoiceItemType.Test && i.ReferenceId.HasValue);
        if (testItem is not null)
        {
            var attempt = await _attemptRepository.GetByInvoiceIdAsync(invoice.Id, request.UserId, cancellationToken);
            if (attempt is not null)
            {
                var effectiveStatus = attempt.Status;
                if (effectiveStatus == TestAttemptStatus.InProgress && attempt.CompletedAt.HasValue)
                {
                    effectiveStatus = TestAttemptStatus.Completed;
                }

                detail = detail with
                {
                    TestAttemptId = attempt.Id,
                    TestAttemptStatus = effectiveStatus
                };
            }
        }

        return Result<InvoiceDetailDto>.Success(detail);
    }
}
