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

namespace LogsDtoCloneTest.Application.Commands.Billing.Wallet;

public sealed record PayInvoiceWithWalletCommand(Guid InvoiceId, string UserId) : ICommand<WalletTransactionListItemDto>;

public sealed class PayInvoiceWithWalletCommandHandler : ICommandHandler<PayInvoiceWithWalletCommand, WalletTransactionListItemDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IMediator _mediator;
    private readonly IAuditContext _auditContext;
    private readonly IFinancialSettingRepository _financialSettingRepository;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PayInvoiceWithWalletCommandHandler> _logger;

    public PayInvoiceWithWalletCommandHandler(
        IInvoiceRepository invoiceRepository,
        IWalletRepository walletRepository,
        IMediator mediator,
        IAuditContext auditContext,
        IFinancialSettingRepository financialSettingRepository,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PayInvoiceWithWalletCommandHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _walletRepository = walletRepository;
        _mediator = mediator;
        _auditContext = auditContext;
        _financialSettingRepository = financialSettingRepository;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
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

        var result = await _invoiceRepository.MutateAsync(
            request.InvoiceId,
            includeDetails: true,
            async (invoice, ct) =>
            {
                if (!string.Equals(invoice.UserId, request.UserId, StringComparison.Ordinal))
                {
                    return Result<WalletTransactionListItemDto>.Failure("این فاکتور به حساب کاربری شما تعلق ندارد.");
                }

                // Ensure VAT from financial settings is applied before charging wallet
                var financialSetting = await _financialSettingRepository.GetCurrentAsync(ct);
                if (financialSetting is not null && financialSetting.ValueAddedTaxPercentage != 0 && invoice.TaxAmount == 0m)
                {
                    var tax = invoice.ItemsTotal * financialSetting.ValueAddedTaxPercentage / 100m;
                    invoice.SetTaxAmount(tax);
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

    private static string GenerateReference(string prefix)
    {
        return ReferenceGenerator.GenerateReadableReference($"WL{prefix}", DateTimeOffset.UtcNow);
    }
}
