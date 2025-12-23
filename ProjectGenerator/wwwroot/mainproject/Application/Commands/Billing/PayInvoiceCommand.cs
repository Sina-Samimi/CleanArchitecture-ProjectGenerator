using System;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Commands.Billing.Wallet;
using MobiRooz.Application.DTOs.Billing;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Enums;
using MobiRooz.Domain.Exceptions;
using MobiRooz.SharedKernel.BaseTypes;
using MediatR;

namespace MobiRooz.Application.Commands.Billing;

public sealed record PayInvoiceCommand(Guid InvoiceId, string UserId, PaymentMethod Method) : ICommand<InvoicePaymentResultDto>;

public sealed class PayInvoiceCommandHandler : ICommandHandler<PayInvoiceCommand, InvoicePaymentResultDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAuditContext _auditContext;
    private readonly ISender _sender;

    public PayInvoiceCommandHandler(
        IInvoiceRepository invoiceRepository,
        IAuditContext auditContext,
        ISender sender)
    {
        _invoiceRepository = invoiceRepository;
        _auditContext = auditContext;
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

                // Prevent wallet payment for wallet charge invoices
                if (IsWalletChargeInvoice(invoice))
                {
                    return Result<InvoicePaymentResultDto>.Failure("فاکتورهای شارژ کیف پول نمی‌توانند با کیف پول پرداخت شوند. لطفاً از درگاه بانکی استفاده کنید.");
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
                // OnlineGateway payments are now handled directly by PaymentController
                // This case should not be reached anymore as WalletController redirects to PaymentController
                return Result<InvoicePaymentResultDto>.Failure("پرداخت از طریق درگاه بانکی باید از طریق PaymentController انجام شود.");
            }

            default:
                return Result<InvoicePaymentResultDto>.Failure("روش پرداخت انتخاب شده پشتیبانی نمی‌شود.");
        }
    }

    private static bool IsWalletChargeInvoice(Domain.Entities.Billing.Invoice invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.ExternalReference))
        {
            return false;
        }

        // Check for both old format (WALLET_CHARGE_) and new format (WCH-)
        return invoice.ExternalReference.StartsWith("WALLET_CHARGE_", StringComparison.OrdinalIgnoreCase) ||
               invoice.ExternalReference.StartsWith("WCH-", StringComparison.OrdinalIgnoreCase);
    }
}
