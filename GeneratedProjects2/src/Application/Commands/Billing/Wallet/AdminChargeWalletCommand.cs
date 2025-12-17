using System;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Billing;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.Domain.Exceptions;
using LogsDtoCloneTest.SharedKernel.BaseTypes;
using LogsDtoCloneTest.SharedKernel.Helpers;

namespace LogsDtoCloneTest.Application.Commands.Billing.Wallet;

public sealed record AdminChargeWalletCommand(
    string UserId,
    decimal Amount,
    string? Currency,
    string? InvoiceTitle,
    string? InvoiceDescription,
    string? TransactionDescription,
    string? PaymentReference,
    PaymentMethod PaymentMethod,
    DateTimeOffset? IssueDate,
    DateTimeOffset? TransactionOccurredAt) : ICommand<AdminWalletChargeResultDto>;

public sealed record AdminWalletChargeResultDto(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid PaymentTransactionId,
    Guid WalletTransactionId,
    decimal Amount,
    string Currency);

public sealed class AdminChargeWalletCommandHandler : ICommandHandler<AdminChargeWalletCommand, AdminWalletChargeResultDto>
{
    private const string DefaultCurrency = "IRT";
    private const string DefaultInvoiceTitle = "شارژ کیف پول";
    private const string AdminGatewayName = "AdminPanel";

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IAuditContext _auditContext;

    public AdminChargeWalletCommandHandler(
        IInvoiceRepository invoiceRepository,
        IWalletRepository walletRepository,
        IAuditContext auditContext)
    {
        _invoiceRepository = invoiceRepository;
        _walletRepository = walletRepository;
        _auditContext = auditContext;
    }

    public async Task<Result<AdminWalletChargeResultDto>> Handle(AdminChargeWalletCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<AdminWalletChargeResultDto>.Failure("شناسه کاربر معتبر نیست.");
        }

        if (request.Amount <= 0)
        {
            return Result<AdminWalletChargeResultDto>.Failure("مبلغ شارژ باید بزرگ‌تر از صفر باشد.");
        }

        var normalizedUserId = request.UserId.Trim();
        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? DefaultCurrency
            : request.Currency!.Trim().ToUpperInvariant();

        var invoiceTitle = string.IsNullOrWhiteSpace(request.InvoiceTitle)
            ? DefaultInvoiceTitle
            : request.InvoiceTitle!.Trim();

        var invoiceDescription = string.IsNullOrWhiteSpace(request.InvoiceDescription)
            ? null
            : request.InvoiceDescription!.Trim();

        var transactionDescription = string.IsNullOrWhiteSpace(request.TransactionDescription)
            ? invoiceDescription ?? invoiceTitle
            : request.TransactionDescription!.Trim();

        var reference = string.IsNullOrWhiteSpace(request.PaymentReference)
            ? GenerateReference("ADM")
            : request.PaymentReference!.Trim();

        var audit = _auditContext.Capture();
        var issueDate = request.IssueDate ?? audit.Timestamp;
        var occurredAt = request.TransactionOccurredAt ?? audit.Timestamp;

        try
        {
            var wallet = await _walletRepository.GetByUserIdAsync(normalizedUserId, cancellationToken);
            var isNewWallet = wallet is null;

            wallet ??= new WalletAccount(normalizedUserId, currency)
            {
                CreatorId = audit.UserId,
                CreateDate = audit.Timestamp,
                UpdateDate = audit.Timestamp,
                UpdaterId = audit.UserId,
                Ip = audit.IpAddress
            };

            if (!string.Equals(wallet.Currency, currency, StringComparison.OrdinalIgnoreCase))
            {
                return Result<AdminWalletChargeResultDto>.Failure("واحد پول کیف پول با درخواست شارژ مطابقت ندارد.");
            }

            // Note: wallet audit fields will be set automatically by AuditInterceptor

            var invoice = new Invoice(
                Invoice.GenerateInvoiceNumber(),
                invoiceTitle,
                invoiceDescription,
                currency,
                normalizedUserId,
                issueDate,
                null,
                0m,
                0m,
                reference);

            // Note: All audit fields will be set automatically by AuditInterceptor

            var item = invoice.AddItem(
                invoiceTitle,
                invoiceDescription,
                InvoiceItemType.Service,
                referenceId: null,
                quantity: 1m,
                unitPrice: request.Amount,
                discountAmount: null,
                attributes: null);

            // Note: All audit fields for item will be set automatically by AuditInterceptor

            var paymentTransaction = invoice.AddTransaction(
                request.Amount,
                request.PaymentMethod,
                TransactionStatus.Succeeded,
                reference,
                AdminGatewayName,
                transactionDescription,
                metadata: null);

            paymentTransaction.OccurredOn(occurredAt);
            // Note: All audit fields for paymentTransaction will be set automatically by AuditInterceptor

            var walletTransaction = wallet.Credit(
                request.Amount,
                reference,
                transactionDescription,
                metadata: null,
                invoice.Id,
                paymentTransaction.Id,
                TransactionStatus.Succeeded,
                occurredAt);

            // Note: walletTransaction audit fields will be set automatically by AuditInterceptor
            walletTransaction.AttachPayment(paymentTransaction.Id);

            if (isNewWallet)
            {
                await _walletRepository.AddAsync(wallet, cancellationToken);
            }
            else
            {
                await _walletRepository.UpdateAsync(wallet, cancellationToken);
            }

            await _invoiceRepository.AddAsync(invoice, cancellationToken);

            return Result<AdminWalletChargeResultDto>.Success(new AdminWalletChargeResultDto(
                invoice.Id,
                invoice.InvoiceNumber,
                paymentTransaction.Id,
                walletTransaction.Id,
                request.Amount,
                currency));
        }
        catch (DomainException ex)
        {
            return Result<AdminWalletChargeResultDto>.Failure(ex.Message);
        }
    }

    private static string GenerateReference(string prefix)
    {
        return ReferenceGenerator.GenerateReadableReference($"WL{prefix}", DateTimeOffset.UtcNow);
    }
}
