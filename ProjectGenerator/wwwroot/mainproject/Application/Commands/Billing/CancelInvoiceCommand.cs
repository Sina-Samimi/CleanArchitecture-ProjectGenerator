using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Commands.Billing;

public sealed record CancelInvoiceCommand(Guid Id) : ICommand<Guid>;

public sealed class CancelInvoiceCommandHandler : ICommandHandler<CancelInvoiceCommand, Guid>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAuditContext _auditContext;

    public CancelInvoiceCommandHandler(IInvoiceRepository invoiceRepository, IAuditContext auditContext)
    {
        _invoiceRepository = invoiceRepository;
        _auditContext = auditContext;
    }

    public async Task<Result<Guid>> Handle(CancelInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty)
        {
            return Result<Guid>.Failure("شناسه فاکتور معتبر نیست.");
        }

        var invoice = await _invoiceRepository.GetByIdAsync(request.Id, cancellationToken, includeDetails: true);
        if (invoice is null)
        {
            return Result<Guid>.Failure("فاکتور مورد نظر یافت نشد.");
        }

        invoice.Cancel();

        // Note: UpdateDate, UpdaterId, and Ip will be set automatically by AuditInterceptor

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

        return Result<Guid>.Success(invoice.Id);
    }
}
