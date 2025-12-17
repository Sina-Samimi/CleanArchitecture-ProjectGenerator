using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Billing;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.Domain.Exceptions;
using LogTableRenameTest.SharedKernel.BaseTypes;
using LogTableRenameTest.SharedKernel.Helpers;
using MediatR;

namespace LogTableRenameTest.Application.Commands.Billing;

public sealed record FrontVerifyTransactionCommand(long TrackingNumber, string TransactionCode, decimal Amount) : ICommand<Guid>;

public sealed class FrontVerifyTransactionCommandHandler : ICommandHandler<FrontVerifyTransactionCommand, Guid>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IAuditContext _auditContext;
    private readonly IMediator _mediator;

    public FrontVerifyTransactionCommandHandler(
        IInvoiceRepository invoiceRepository,
        IWalletRepository walletRepository,
        IAuditContext auditContext,
        IMediator mediator)
    {
        _invoiceRepository = invoiceRepository;
        _walletRepository = walletRepository;
        _auditContext = auditContext;
        _mediator = mediator;
    }

    public async Task<Result<Guid>> Handle(FrontVerifyTransactionCommand request, CancellationToken cancellationToken)
    {
        if (request.TrackingNumber <= 0)
        {
            return Result<Guid>.Failure("شماره پیگیری نامعتبر است.");
        }

        // Search for invoice by tracking number stored in PaymentTransaction.Reference
        var invoice = await _invoiceRepository.GetByTrackingNumberAsync(request.TrackingNumber, cancellationToken);
        if (invoice is null)
        {
            return Result<Guid>.Failure("فاکتور یافت نشد. لطفاً با پشتیبانی تماس بگیرید.");
        }

        // Use the actual amount paid from gateway, but ensure it doesn't exceed outstanding amount
        // If amount is invalid (zero or negative), use outstanding amount as fallback
        var paymentAmount = request.Amount > 0 && request.Amount <= invoice.OutstandingAmount
            ? request.Amount
            : invoice.OutstandingAmount;

        // Find and update the existing payment transaction by tracking number (stored in Reference)
        var trackingNumberStr = request.TrackingNumber.ToString();
        var result = await _invoiceRepository.MutateAsync(
            invoice.Id,
            includeDetails: true,
            (inv, ct) =>
            {
                // Find the existing transaction by tracking number
                var existingTransaction = inv.Transactions
                    .FirstOrDefault(t => t.Reference == trackingNumberStr && t.Method == PaymentMethod.OnlineGateway);

                if (existingTransaction is null)
                {
                    return Task.FromResult(Result<Guid>.Failure("تراکنش پرداخت یافت نشد."));
                }

                try
                {
                    var updatedTransaction = inv.UpdateTransaction(
                        existingTransaction.Id,
                        TransactionStatus.Succeeded,
                        $"پرداخت آنلاین فاکتور {inv.InvoiceNumber} - کد پیگیری: {request.TransactionCode}",
                        request.TransactionCode, // Store transaction code in metadata
                        DateTimeOffset.Now,
                        paymentAmount);

                    return Task.FromResult(Result<Guid>.Success(updatedTransaction.Id));
                }
                catch (DomainException ex)
                {
                    return Task.FromResult(Result<Guid>.Failure(ex.Message));
                }
            },
            cancellationToken,
            notFoundMessage: "فاکتور مورد نظر یافت نشد.");

        // Charge wallet if invoice is for wallet charge (after transaction is committed)
        if (result.IsSuccess)
        {
            // Reload invoice to get updated status
            var updatedInvoice = await _invoiceRepository.GetByIdAsync(invoice.Id, cancellationToken, includeDetails: true);
            if (updatedInvoice is not null && 
                IsWalletChargeInvoice(updatedInvoice) && 
                updatedInvoice.Status == InvoiceStatus.Paid &&
                !string.IsNullOrWhiteSpace(updatedInvoice.UserId))
            {
                try
                {
                    var chargeAmount = updatedInvoice.GrandTotal;
                    var chargeDescription = $"شارژ کیف پول از طریق فاکتور {updatedInvoice.InvoiceNumber}";

                    // Find the payment transaction ID
                    var paymentTransaction = updatedInvoice.Transactions
                        .FirstOrDefault(t => t.Reference == trackingNumberStr && 
                                           t.Method == PaymentMethod.OnlineGateway &&
                                           t.Status == TransactionStatus.Succeeded);

                    if (paymentTransaction is not null)
                    {
                        var chargeResult = await ChargeWalletFromInvoiceAsync(
                            updatedInvoice.UserId,
                            chargeAmount,
                            updatedInvoice.Currency,
                            chargeDescription,
                            updatedInvoice.Id,
                            paymentTransaction.Id,
                            cancellationToken);

                        // Log warning if charge fails, but don't fail the payment verification
                        // The invoice payment is already confirmed
                    }
                }
                catch (Exception)
                {
                    // Log error but don't fail the payment verification
                    // The invoice payment is already confirmed, wallet charge can be done manually if needed
                }
            }
        }

        return result;
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

    private async Task<Result<WalletTransactionListItemDto>> ChargeWalletFromInvoiceAsync(
        string userId,
        decimal amount,
        string currency,
        string description,
        Guid invoiceId,
        Guid paymentTransactionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var wallet = await _walletRepository.GetByUserIdAsync(userId.Trim(), cancellationToken);
            var audit = _auditContext.Capture();

            var isNewAccount = wallet is null;
            var account = wallet ?? new Domain.Entities.Billing.WalletAccount(userId.Trim(), currency)
            {
                CreatorId = audit.UserId,
                CreateDate = audit.Timestamp,
                UpdateDate = audit.Timestamp,
                Ip = audit.IpAddress,
                UpdaterId = audit.UserId
            };

            if (!isNewAccount && !string.Equals(account.Currency, currency, StringComparison.OrdinalIgnoreCase))
            {
                return Result<WalletTransactionListItemDto>.Failure("واحد پول کیف پول با واحد درخواستی مغایرت دارد.");
            }

            var reference = ReferenceGenerator.GenerateReadableReference("WLDEP", DateTimeOffset.Now);
            var transaction = account.Credit(
                amount,
                reference,
                description,
                metadata: null,
                invoiceId: invoiceId,
                paymentTransactionId: paymentTransactionId,
                TransactionStatus.Succeeded,
                audit.Timestamp);

            if (isNewAccount)
            {
                await _walletRepository.AddAsync(account, cancellationToken);
            }
            else
            {
                await _walletRepository.UpdateAsync(account, cancellationToken);
            }

            return Result<WalletTransactionListItemDto>.Success(transaction.ToListItemDto());
        }
        catch (DomainException ex)
        {
            return Result<WalletTransactionListItemDto>.Failure(ex.Message);
        }
    }
}
