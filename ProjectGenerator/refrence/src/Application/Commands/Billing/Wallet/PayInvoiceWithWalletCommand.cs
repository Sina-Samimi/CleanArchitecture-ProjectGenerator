using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Billing;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.Domain.Exceptions;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Billing.Wallet;

public sealed record PayInvoiceWithWalletCommand(Guid InvoiceId, string UserId) : ICommand<WalletTransactionListItemDto>;

public sealed class PayInvoiceWithWalletCommandHandler : ICommandHandler<PayInvoiceWithWalletCommand, WalletTransactionListItemDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IAuditContext _auditContext;

    public PayInvoiceWithWalletCommandHandler(
        IInvoiceRepository invoiceRepository,
        IWalletRepository walletRepository,
        IAuditContext auditContext)
    {
        _invoiceRepository = invoiceRepository;
        _walletRepository = walletRepository;
        _auditContext = auditContext;
    }

    public async Task<Result<WalletTransactionListItemDto>> Handle(PayInvoiceWithWalletCommand request, CancellationToken cancellationToken)
    {
        if (request.InvoiceId == Guid.Empty)
        {
            return Result<WalletTransactionListItemDto>.Failure("شناسه فاکتور معتبر نیست.");
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<WalletTransactionListItemDto>.Failure("شناسه کاربر معتبر نیست.");
        }

        return await _invoiceRepository.MutateAsync(
            request.InvoiceId,
            includeDetails: true,
            async (invoice, ct) =>
            {
                if (!string.Equals(invoice.UserId, request.UserId, StringComparison.Ordinal))
                {
                    return Result<WalletTransactionListItemDto>.Failure("این فاکتور به حساب کاربری شما تعلق ندارد.");
                }

                var outstanding = invoice.OutstandingAmount;
                if (outstanding <= 0)
                {
                    return Result<WalletTransactionListItemDto>.Failure("این فاکتور قبلاً تسویه شده است.");
                }

                var wallet = await _walletRepository.GetByUserIdAsync(request.UserId.Trim(), ct);
                if (wallet is null)
                {
                    return Result<WalletTransactionListItemDto>.Failure("کیف پول فعالی برای شما یافت نشد.");
                }

                if (!string.Equals(wallet.Currency, invoice.Currency, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<WalletTransactionListItemDto>.Failure("واحد پول فاکتور با کیف پول شما سازگار نیست.");
                }

                if (wallet.IsLocked)
                {
                    return Result<WalletTransactionListItemDto>.Failure("کیف پول شما در حال حاضر قفل است.");
                }

                if (wallet.Balance < outstanding)
                {
                    return Result<WalletTransactionListItemDto>.Failure("موجودی کیف پول برای پرداخت فاکتور کافی نیست.");
                }

                try
                {
                    var audit = _auditContext.Capture();
                    // Note: wallet audit fields will be set automatically by AuditInterceptor

                    var reference = GenerateReference("INV");
                    var description = $"پرداخت فاکتور {invoice.InvoiceNumber}";

                    var walletTransaction = wallet.Debit(
                        outstanding,
                        reference,
                        description,
                        metadata: invoice.InvoiceNumber,
                        invoiceId: invoice.Id,
                        paymentTransactionId: null,
                        TransactionStatus.Succeeded,
                        audit.Timestamp);

                    // Note: walletTransaction audit fields will be set automatically by AuditInterceptor

                    var payment = invoice.AddTransaction(
                        outstanding,
                        PaymentMethod.Wallet,
                        TransactionStatus.Succeeded,
                        reference,
                        "Wallet",
                        description,
                        walletTransaction.Id.ToString());

                    // Note: All audit fields will be set automatically by AuditInterceptor

                    walletTransaction.AttachPayment(payment.Id);

                    return Result<WalletTransactionListItemDto>.Success(walletTransaction.ToListItemDto());
                }
                catch (DomainException ex)
                {
                    return Result<WalletTransactionListItemDto>.Failure(ex.Message);
                }
            },
            cancellationToken);
    }

    private static string GenerateReference(string prefix)
    {
        var token = Guid.NewGuid().ToString("N").ToUpperInvariant();
        return $"WL-{prefix}-{token[..8]}";
    }
}
