using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.Domain.Enums;
using Attar.Domain.Exceptions;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Billing;

public sealed record RecordInvoiceTransactionCommand(
    Guid InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    TransactionStatus Status,
    string Reference,
    string? GatewayName,
    string? Description,
    string? Metadata,
    DateTimeOffset? OccurredAt) : ICommand<Guid>;

public sealed class RecordInvoiceTransactionCommandHandler : ICommandHandler<RecordInvoiceTransactionCommand, Guid>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAuditContext _auditContext;

    public RecordInvoiceTransactionCommandHandler(IInvoiceRepository invoiceRepository, IAuditContext auditContext)
    {
        _invoiceRepository = invoiceRepository;
        _auditContext = auditContext;
    }

    public async Task<Result<Guid>> Handle(RecordInvoiceTransactionCommand request, CancellationToken cancellationToken)
    {
        if (request.InvoiceId == Guid.Empty)
        {
            return Result<Guid>.Failure("شناسه فاکتور معتبر نیست.");
        }

        if (request.Amount <= 0)
        {
            return Result<Guid>.Failure("مبلغ تراکنش باید بیشتر از صفر باشد.");
        }

        if (string.IsNullOrWhiteSpace(request.Reference))
        {
            return Result<Guid>.Failure("شناسه مرجع تراکنش الزامی است.");
        }

        var normalizedReference = request.Reference.Trim();

        var result = await _invoiceRepository.MutateAsync(
            request.InvoiceId,
            includeDetails: true,
            (invoice, ct) =>
            {
                if (invoice.Transactions.Any(transaction => transaction.Reference.Equals(normalizedReference, StringComparison.OrdinalIgnoreCase)))
                {
                    return Task.FromResult(Result<Guid>.Failure("تراکنشی با این شناسه مرجع قبلاً ثبت شده است."));
                }

                try
                {
                    var transaction = invoice.AddTransaction(
                        request.Amount,
                        request.Method,
                        request.Status,
                        normalizedReference,
                        request.GatewayName,
                        request.Description,
                        request.Metadata);

                    if (request.OccurredAt is not null)
                    {
                        transaction.OccurredOn(request.OccurredAt.Value);
                    }

                    // Note: All audit fields will be set automatically by AuditInterceptor

                    return Task.FromResult(Result<Guid>.Success(transaction.Id));
                }
                catch (DomainException ex)
                {
                    return Task.FromResult(Result<Guid>.Failure(ex.Message));
                }
            },
            cancellationToken,
            notFoundMessage: "فاکتور مورد نظر یافت نشد.");

        return result;
    }
}
