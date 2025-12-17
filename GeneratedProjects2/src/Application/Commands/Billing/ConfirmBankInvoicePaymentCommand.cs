using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Commands.Billing;
using LogsDtoCloneTest.Application.Commands.Catalog;
using LogsDtoCloneTest.Application.DTOs.Billing;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.Domain.Exceptions;
using LogsDtoCloneTest.SharedKernel.BaseTypes;
using LogsDtoCloneTest.SharedKernel.Helpers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LogsDtoCloneTest.Application.Commands.Billing;

public sealed record ConfirmBankInvoicePaymentCommand(Guid InvoiceId, string Reference) : ICommand<InvoicePaymentResultDto>;

public sealed class ConfirmBankInvoicePaymentCommandHandler : ICommandHandler<ConfirmBankInvoicePaymentCommand, InvoicePaymentResultDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IBankingGatewayService _bankingGatewayService;
    private readonly IAuditContext _auditContext;
    private readonly IWalletRepository _walletRepository;
    private readonly IMediator _mediator;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ConfirmBankInvoicePaymentCommandHandler> _logger;

    public ConfirmBankInvoicePaymentCommandHandler(
        IInvoiceRepository invoiceRepository,
        IBankingGatewayService bankingGatewayService,
        IAuditContext auditContext,
        IWalletRepository walletRepository,
        IMediator mediator,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ConfirmBankInvoicePaymentCommandHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _bankingGatewayService = bankingGatewayService;
        _auditContext = auditContext;
        _walletRepository = walletRepository;
        _mediator = mediator;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
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

                    // If payment succeeded and invoice is for wallet charge, charge the wallet
                    if (verification.Status == TransactionStatus.Succeeded && 
                        IsWalletChargeInvoice(invoice) && 
                        !string.IsNullOrWhiteSpace(invoice.UserId))
                    {
                        // Get the charge amount from invoice (should be the grand total)
                        var chargeAmount = invoice.GrandTotal;
                        var chargeDescription = $"شارژ کیف پول از طریق فاکتور {invoice.InvoiceNumber}";

                        // Charge wallet with invoice and payment transaction linked
                        var chargeResult = await ChargeWalletFromInvoiceAsync(
                            invoice.UserId,
                            chargeAmount,
                            invoice.Currency,
                            chargeDescription,
                            invoice.Id,
                            updatedTransaction.Id,
                            ct);

                        if (!chargeResult.IsSuccess)
                        {
                            // Log warning but don't fail the payment confirmation
                            // The invoice payment is already confirmed, wallet charge can be done manually if needed
                            // In production, you might want to use a background job or retry mechanism
                        }
                    }

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

        // Charge seller share after transaction is committed
        if (result.IsSuccess)
        {
            // Check if invoice is now fully paid (after transaction commit)
            var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
            if (invoice is not null && invoice.Status == InvoiceStatus.Paid)
            {
                // Use a new scope to avoid DataReader conflicts and ensure proper dependency injection
                // Execute synchronously to ensure it completes
                try
                {
                    await using var scope = _serviceScopeFactory.CreateAsyncScope();
                    var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    
                    // Small delay to ensure transaction is committed
                    await Task.Delay(200, cancellationToken);
                    
                    var chargeResult = await scopedMediator.Send(new ChargeSellerShareCommand(request.InvoiceId), cancellationToken);
                    
                    if (!chargeResult.IsSuccess)
                    {
                        _logger.LogWarning("Failed to charge seller share for invoice {InvoiceId}: {Error}", 
                            request.InvoiceId, chargeResult.Error);
                    }
                    else
                    {
                        _logger.LogInformation("Successfully charged seller share for invoice {InvoiceId}", request.InvoiceId);
                    }

                    // Reduce product stock for paid invoice items
                    try
                    {
                        foreach (var item in invoice.Items)
                        {
                            if (item.ItemType == InvoiceItemType.Product && 
                                item.ReferenceId.HasValue)
                            {
                                var reduceStockResult = await scopedMediator.Send(
                                    new ReduceProductStockCommand(
                                        item.ReferenceId.Value,
                                        item.VariantId,
                                        (int)item.Quantity),
                                    cancellationToken);

                                if (!reduceStockResult.IsSuccess)
                                {
                                    _logger.LogWarning("Failed to reduce stock for product {ProductId} in invoice {InvoiceId}: {Error}", 
                                        item.ReferenceId.Value, request.InvoiceId, reduceStockResult.Error);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reducing product stock for invoice {InvoiceId}", request.InvoiceId);
                        // Don't fail the payment if stock reduction fails
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error charging seller share for invoice {InvoiceId}", request.InvoiceId);
                    // Don't fail the payment if seller share charging fails
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

    private async Task<Result<Application.DTOs.Billing.WalletTransactionListItemDto>> ChargeWalletFromInvoiceAsync(
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
                return Result<Application.DTOs.Billing.WalletTransactionListItemDto>.Failure("واحد پول کیف پول با واحد درخواستی مغایرت دارد.");
            }

            var reference = GenerateReference("DEP");
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

            return Result<Application.DTOs.Billing.WalletTransactionListItemDto>.Success(transaction.ToListItemDto());
        }
        catch (DomainException ex)
        {
            return Result<Application.DTOs.Billing.WalletTransactionListItemDto>.Failure(ex.Message);
        }
    }

    private static string GenerateReference(string prefix)
    {
        return ReferenceGenerator.GenerateReadableReference($"WL{prefix}", DateTimeOffset.UtcNow);
    }
}
