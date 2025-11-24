using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Commands.Billing.Wallet;
using Arsis.Application.DTOs.Billing;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.Domain.Exceptions;
using Arsis.SharedKernel.BaseTypes;
using MediatR;

namespace Arsis.Application.Commands.Billing;

public sealed record PayInvoiceCommand(Guid InvoiceId, string UserId, PaymentMethod Method) : ICommand<InvoicePaymentResultDto>;

public sealed class PayInvoiceCommandHandler : ICommandHandler<PayInvoiceCommand, InvoicePaymentResultDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAuditContext _auditContext;
    private readonly IBankingGatewayService _bankingGatewayService;
    private readonly ISender _sender;

    public PayInvoiceCommandHandler(
        IInvoiceRepository invoiceRepository,
        IAuditContext auditContext,
        IBankingGatewayService bankingGatewayService,
        ISender sender)
    {
        _invoiceRepository = invoiceRepository;
        _auditContext = auditContext;
        _bankingGatewayService = bankingGatewayService;
        _sender = sender;
    }

    public async Task<Result<InvoicePaymentResultDto>> Handle(PayInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (request.InvoiceId == Guid.Empty)
        {
            return Result<InvoicePaymentResultDto>.Failure("شناسه فاکتور معتبر نیست.");
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<InvoicePaymentResultDto>.Failure("شناسه کاربر معتبر نیست.");
        }

        switch (request.Method)
        {
            case PaymentMethod.Wallet:
            {
                var invoice = await _invoiceRepository.GetByIdForUserAsync(
                    request.InvoiceId,
                    request.UserId,
                    cancellationToken);

                if (invoice is null)
                {
                    return Result<InvoicePaymentResultDto>.Failure("فاکتور مورد نظر یافت نشد.");
                }

                var walletResult = await _sender.Send(
                    new PayInvoiceWithWalletCommand(request.InvoiceId, request.UserId),
                    cancellationToken);

                if (!walletResult.IsSuccess)
                {
                    return Result<InvoicePaymentResultDto>.Failure(walletResult.Error!);
                }

                if (walletResult.Value is null)
                {
                    return Result<InvoicePaymentResultDto>.Failure("خطا در ثبت پرداخت کیف پول رخ داد.");
                }

                return Result<InvoicePaymentResultDto>.Success(
                    InvoicePaymentResultDto.FromWallet(invoice.Id, invoice.InvoiceNumber, walletResult.Value));
            }

            case PaymentMethod.OnlineGateway:
            {
                var invoice = await _invoiceRepository.GetByIdForUserAsync(
                    request.InvoiceId,
                    request.UserId,
                    cancellationToken,
                    includeDetails: true);

                if (invoice is null)
                {
                    return Result<InvoicePaymentResultDto>.Failure("فاکتور مورد نظر یافت نشد.");
                }

                if (invoice.OutstandingAmount <= 0)
                {
                    return Result<InvoicePaymentResultDto>.Failure("این فاکتور قبلاً تسویه شده است.");
                }

                var paymentRequest = new BankPaymentRequest(
                    invoice.Id,
                    invoice.InvoiceNumber,
                    invoice.Title,
                    invoice.OutstandingAmount,
                    invoice.Currency,
                    invoice.UserId ?? request.UserId,
                    invoice.Description);

                var sessionResult = await _bankingGatewayService.CreatePaymentSessionAsync(paymentRequest, cancellationToken);
                if (!sessionResult.IsSuccess)
                {
                    return Result<InvoicePaymentResultDto>.Failure(sessionResult.Error!);
                }

                try
                {
                    var session = sessionResult.Value;
                    if (session is null)
                    {
                        return Result<InvoicePaymentResultDto>.Failure("در ایجاد نشست پرداخت بانکی خطایی رخ داد.");
                    }
                    var transaction = invoice.AddTransaction(
                        session.Amount,
                        PaymentMethod.OnlineGateway,
                        TransactionStatus.Pending,
                        session.Reference,
                        session.GatewayName,
                        session.Description,
                        session.SessionToken);

                    // Note: All audit fields will be set automatically by AuditInterceptor

                    await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

                    return Result<InvoicePaymentResultDto>.Success(
                        InvoicePaymentResultDto.FromBankSession(invoice.Id, invoice.InvoiceNumber, session));
                }
                catch (DomainException ex)
                {
                    return Result<InvoicePaymentResultDto>.Failure(ex.Message);
                }
            }

            default:
                return Result<InvoicePaymentResultDto>.Failure("روش پرداخت انتخاب شده پشتیبانی نمی‌شود.");
        }
    }
}
