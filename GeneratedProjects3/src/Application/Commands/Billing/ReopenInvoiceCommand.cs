using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Commands.Billing;

public sealed record ReopenInvoiceCommand(Guid Id) : ICommand<Guid>;

public sealed class ReopenInvoiceCommandHandler : ICommandHandler<ReopenInvoiceCommand, Guid>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAuditContext _auditContext;

    public ReopenInvoiceCommandHandler(IInvoiceRepository invoiceRepository, IAuditContext auditContext)
    {
        _invoiceRepository = invoiceRepository;
        _auditContext = auditContext;
    }

    public async Task<Result<Guid>> Handle(ReopenInvoiceCommand request, CancellationToken cancellationToken)
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

        invoice.Reopen();
        invoice.EvaluateStatus(null);

        // Note: UpdateDate, UpdaterId, and Ip will be set automatically by AuditInterceptor

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

        return Result<Guid>.Success(invoice.Id);
    }
}
