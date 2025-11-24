using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Billing;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.Domain.Exceptions;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Billing;

public sealed record ConfirmBankInvoicePaymentCommand(Guid InvoiceId, string Reference) : ICommand<InvoicePaymentResultDto>;

public sealed class ConfirmBankInvoicePaymentCommandHandler : ICommandHandler<ConfirmBankInvoicePaymentCommand, InvoicePaymentResultDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IBankingGatewayService _bankingGatewayService;
    private readonly IAuditContext _auditContext;

    public ConfirmBankInvoicePaymentCommandHandler(
        IInvoiceRepository invoiceRepository,
        IBankingGatewayService bankingGatewayService,
        IAuditContext auditContext)
    {
        _invoiceRepository = invoiceRepository;
        _bankingGatewayService = bankingGatewayService;
        _auditContext = auditContext;
    }

    public async Task<Result<InvoicePaymentResultDto>> Handle(ConfirmBankInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        if (request.InvoiceId == Guid.Empty)
        {
            return Result<InvoicePaymentResultDto>.Failure("شناسه فاکتور معتبر نیست.");
        }

        if (string.IsNullOrWhiteSpace(request.Reference))
        {
            return Result<InvoicePaymentResultDto>.Failure("شناسه پیگیری درگاه بانکی معتبر نیست.");
        }

        var normalizedReference = request.Reference.Trim();

        var result = await _invoiceRepository.MutateAsync(
            request.InvoiceId,
            includeDetails: true,
            async (invoice, ct) =>
            {
                var transaction = invoice.Transactions
                    .FirstOrDefault(t => t.Reference.Equals(normalizedReference, StringComparison.OrdinalIgnoreCase));

                if (transaction is null)
                {
                    return Result<InvoicePaymentResultDto>.Failure("تراکنش بانکی یافت نشد.");
                }

                if (transaction.Method != PaymentMethod.OnlineGateway)
                {
                    return Result<InvoicePaymentResultDto>.Failure("این تراکنش مربوط به درگاه بانکی نیست.");
                }

                var verificationResult = await _bankingGatewayService.VerifyPaymentAsync(normalizedReference, ct);
                if (!verificationResult.IsSuccess)
                {
                    return Result<InvoicePaymentResultDto>.Failure(verificationResult.Error!);
                }

                var verification = verificationResult.Value;
                if (verification is null)
                {
                    return Result<InvoicePaymentResultDto>.Failure("پاسخ درگاه بانکی نامعتبر است.");
                }

                try
                {
                    var updatedTransaction = invoice.UpdateTransaction(
                        transaction.Id,
                        verification.Status,
                        verification.Message,
                        verification.TrackingCode,
                        verification.ProcessedAt,
                        verification.Amount);

                    // Note: All audit fields will be set automatically by AuditInterceptor

                    return Result<InvoicePaymentResultDto>.Success(
                        InvoicePaymentResultDto.FromBankReceipt(invoice.Id, invoice.InvoiceNumber, verification));
                }
                catch (DomainException ex)
                {
                    return Result<InvoicePaymentResultDto>.Failure(ex.Message);
                }
            },
            cancellationToken,
            notFoundMessage: "فاکتور مورد نظر یافت نشد.");

        return result;
    }
}
