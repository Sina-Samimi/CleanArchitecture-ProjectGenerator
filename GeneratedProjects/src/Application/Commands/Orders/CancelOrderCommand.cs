using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Notifications;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Application.Queries.Admin.FinancialSettings;
using TestAttarClone.Application.Commands.Notifications;
using TestAttarClone.Domain.Constants;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.BaseTypes;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TestAttarClone.Application.Commands.Orders
{
    public sealed record CancelOrderCommand(Guid InvoiceId, string? Reason = null) : ICommand<bool>;

    // Temporarily disabled - commented out for future implementation
    /*
    public sealed class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand, bool>
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IShipmentTrackingRepository _trackingRepository;
        private readonly IProductRepository _productRepository;
        private readonly ISellerProfileRepository _sellerProfileRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IMediator _mediator;
        private readonly ILogger<CancelOrderCommandHandler> _logger;

        public CancelOrderCommandHandler(
            IInvoiceRepository invoiceRepository,
            IShipmentTrackingRepository trackingRepository,
            IProductRepository productRepository,
            ISellerProfileRepository sellerProfileRepository,
            IWalletRepository walletRepository,
            IMediator mediator,
            ILogger<CancelOrderCommandHandler> logger)
        {
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _trackingRepository = trackingRepository ?? throw new ArgumentNullException(nameof(trackingRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _sellerProfileRepository = sellerProfileRepository ?? throw new ArgumentNullException(nameof(sellerProfileRepository));
            _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.InvoiceId == Guid.Empty)
                return Result<bool>.Failure("شناسه فاکتور معتبر نیست.");

            _logger.LogInformation("Starting cancellation for invoice {InvoiceId}", request.InvoiceId);

            var financialSettingsResult = await _mediator.Send(new GetFinancialSettingsQuery(), cancellationToken);
            if (!financialSettingsResult.IsSuccess || financialSettingsResult.Value is null)
            {
                _logger.LogWarning("Failed to load financial settings while cancelling invoice {InvoiceId}", request.InvoiceId);
                return Result<bool>.Failure("خطا در دریافت تنظیمات مالی.");
            }

            var financialSettings = financialSettingsResult.Value;

            var result = await _invoiceRepository.MutateAsync<bool>(request.InvoiceId, includeDetails: true,
                async (invoice, ct) =>
                {
                    var trackings = await _trackingRepository.GetByInvoiceIdAsync(invoice.Id, ct);
                    if (trackings.Any(t => t.Status == ShipmentStatus.Delivered))
                    {
                        _logger.LogInformation("Invoice {InvoiceId} cannot be cancelled because a delivery exists", invoice.Id);
                        return Result<bool>.Failure("این سفارش قابل لغو نیست زیرا کالا توسط مشتری دریافت شده است.");
                    }

                    if (invoice.Status == InvoiceStatus.Cancelled)
                    {
                        _logger.LogInformation("Invoice {InvoiceId} is already cancelled", invoice.Id);
                        return Result<bool>.Success(true);
                    }

                    var productItems = invoice.Items
                        .Where(i => i.ItemType == InvoiceItemType.Product && i.ReferenceId.HasValue)
                        .ToList();

                    var productIds = productItems.Select(i => i.ReferenceId!.Value).Distinct().ToList();
                    var products = await _productRepository.GetByIdsAsync(productIds, ct);
                    var productMap = products.ToDictionary(p => p.Id);

                    var sellerDebits = new Dictionary<string, decimal>();
                    decimal totalPlatformCommission = 0m;

                    foreach (var item in productItems)
                    {
                        if (!item.ReferenceId.HasValue || !productMap.TryGetValue(item.ReferenceId.Value, out var product))
                            continue;
                        if (string.IsNullOrWhiteSpace(product.SellerId))
                            continue;

                        var sellerId = product.SellerId;
                        var sellerProfile = await _sellerProfileRepository.GetByUserIdAsync(sellerId, ct);
                        var sellerSharePercentage = sellerProfile?.SellerSharePercentage ?? financialSettings.SellerProductSharePercentage;

                        var amount = item.Total;
                        decimal sellerShare;
                        if (financialSettings.CommissionCalculationMethod == PlatformCommissionCalculationMethod.DeductFromSeller)
                        {
                            var baseSellerShare = amount * sellerSharePercentage / 100m;
                            var platformCommission = amount * financialSettings.PlatformCommissionPercentage / 100m;
                            sellerShare = decimal.Round(baseSellerShare - platformCommission, 2, MidpointRounding.AwayFromZero);
                            totalPlatformCommission += decimal.Round(platformCommission, 2, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            sellerShare = decimal.Round(amount * sellerSharePercentage / 100m, 2, MidpointRounding.AwayFromZero);
                            totalPlatformCommission += decimal.Round(amount - sellerShare, 2, MidpointRounding.AwayFromZero);
                        }

                        if (sellerShare > 0)
                        {
                            sellerDebits.TryGetValue(sellerId, out var acc);
                            sellerDebits[sellerId] = acc + sellerShare;
                        }
                    }

                    var refundAmount = invoice.PaidAmount;
                    var isPaid = refundAmount > 0;

                    // اگر فاکتور پرداخت نشده باشد، فقط فاکتور را لغو می‌کنیم
                    if (!isPaid)
                    {
                        invoice.Cancel();

                        try
                        {
                            if (invoice.UserId is not null)
                            {
                                var buyerNotification = new CreateNotificationDto(
                                    "لغو سفارش",
                                    $"سفارش شما {invoice.InvoiceNumber} لغو شد.",
                                    NotificationType.System,
                                    NotificationPriority.Normal,
                                    null,
                                    new Application.DTOs.Notifications.NotificationFilterDto(new[] { invoice.UserId }));

                                await _mediator.Send(new CreateNotificationCommand(buyerNotification), ct);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send notifications for cancelled invoice {InvoiceId}", invoice.Id);
                        }

                        _logger.LogInformation("Invoice {InvoiceId} cancelled successfully (unpaid)", invoice.Id);
                        return Result<bool>.Success(true);
                    }

                    // اگر فاکتور پرداخت شده باشد، فاکتور بازگشت وجه می‌سازیم و کسورات انجام می‌دهیم
                    var systemUserId = SystemUsers.AutomationId;
                    var systemWallet = await _walletRepository.GetByUserIdAsync(systemUserId, ct);

                    foreach (var kv in sellerDebits)
                    {
                        var wallet = await _walletRepository.GetByUserIdAsync(kv.Key, ct);
                        if (wallet is null || wallet.Balance < kv.Value)
                        {
                            _logger.LogWarning("Seller {SellerId} has insufficient balance during cancel of invoice {InvoiceId}", kv.Key, invoice.Id);
                            return Result<bool>.Failure($"موجودی کیف‌پول فروشنده {kv.Key} برای کسر مبلغ کافی نیست.");
                        }
                    }

                    if (totalPlatformCommission > 0 && (systemWallet is null || systemWallet.Balance < totalPlatformCommission))
                    {
                        _logger.LogWarning("System wallet has insufficient balance for platform commission during cancel of invoice {InvoiceId}", invoice.Id);
                        return Result<bool>.Failure("موجودی کیف‌پول پلتفرم برای کسر کمیسیون کافی نیست.");
                    }

                    var cancelInvoice = new Domain.Entities.Billing.Invoice(
                        Domain.Entities.Billing.Invoice.GenerateInvoiceNumber(),
                        "لغو سفارش",
                        $"لغو فاکتور {invoice.InvoiceNumber}",
                        invoice.Currency,
                        invoice.UserId,
                        DateTimeOffset.UtcNow,
                        null,
                        0m,
                        0m,
                        null);

                    foreach (var kv in sellerDebits)
                    {
                        cancelInvoice.AddItem($"کسورات بابت لغو سفارش از فروشنده {kv.Key}", null, InvoiceItemType.Service, null, 1m, kv.Value, null, null);
                    }

                    if (totalPlatformCommission > 0)
                    {
                        cancelInvoice.AddItem("کسورات کمیسیون پلتفرم بابت لغو سفارش", null, InvoiceItemType.Service, null, 1m, totalPlatformCommission, null, null);
                    }

                    invoice.Cancel();

                    // اضافه کردن فاکتور لغو به دیتابیس
                    await _invoiceRepository.AddAsync(cancelInvoice, ct);

                    // کسر از کیف‌پول‌ها و اضافه کردن تراکنش به فاکتور لغو
                    foreach (var kv in sellerDebits)
                    {
                        var wallet = await _walletRepository.GetByUserIdAsync(kv.Key, ct);
                        var reference = $"CANCEL_SELLER_{kv.Key}_INV_{invoice.Id}";
                        wallet!.Debit(kv.Value, reference, $"کسورات بابت لغو سفارش {invoice.InvoiceNumber}", metadata: cancelInvoice.InvoiceNumber, invoiceId: cancelInvoice.Id, paymentTransactionId: null, TransactionStatus.Succeeded, DateTimeOffset.UtcNow);
                        
                        // اضافه کردن تراکنش به فاکتور لغو
                        cancelInvoice.AddTransaction(
                            kv.Value,
                            PaymentMethod.Wallet,
                            TransactionStatus.Succeeded,
                            reference,
                            null,
                            $"کسورات بابت لغو سفارش از فروشنده {kv.Key}",
                            cancelInvoice.InvoiceNumber);
                    }

                    if (totalPlatformCommission > 0)
                    {
                        var reference = $"CANCEL_PLATFORM_INV_{invoice.Id}";
                        systemWallet!.Debit(totalPlatformCommission, reference, $"کسورات پلتفرم بابت لغو سفارش {invoice.InvoiceNumber}", metadata: cancelInvoice.InvoiceNumber, invoiceId: cancelInvoice.Id, paymentTransactionId: null, TransactionStatus.Succeeded, DateTimeOffset.UtcNow);
                        
                        // اضافه کردن تراکنش به فاکتور لغو
                        cancelInvoice.AddTransaction(
                            totalPlatformCommission,
                            PaymentMethod.Wallet,
                            TransactionStatus.Succeeded,
                            reference,
                            null,
                            "کسورات کمیسیون پلتفرم بابت لغو سفارش",
                            cancelInvoice.InvoiceNumber);
                    }

                    // بازپرداخت به خریدار
                    if (invoice.UserId is not null)
                    {
                        var buyer = await _walletRepository.GetByUserIdAsync(invoice.UserId, ct);
                        var reference = $"REFUND_INV_{invoice.Id}";
                        var buyerWallet = buyer ?? new Domain.Entities.Billing.WalletAccount(invoice.UserId, invoice.Currency);
                        buyerWallet.Credit(refundAmount, reference, $"بازپرداخت لغو سفارش {invoice.InvoiceNumber}", metadata: invoice.InvoiceNumber, invoiceId: invoice.Id, paymentTransactionId: null, TransactionStatus.Succeeded, DateTimeOffset.UtcNow);
                    }

                    // به‌روزرسانی فاکتور لغو برای بستن آن (چون کسورات انجام شده است)
                    cancelInvoice.EvaluateStatus(DateTimeOffset.UtcNow);
                    await _invoiceRepository.UpdateAsync(cancelInvoice, ct);

                    try
                    {
                        if (invoice.UserId is not null)
                        {
                            var buyerNotification = new CreateNotificationDto(
                                "لغو سفارش",
                                $"سفارش شما {invoice.InvoiceNumber} لغو شد. مبلغ {refundAmount} به کیف‌پول شما بازگشت داده شد.",
                                NotificationType.System,
                                NotificationPriority.Normal,
                                null,
                                new Application.DTOs.Notifications.NotificationFilterDto(new[] { invoice.UserId }));

                            await _mediator.Send(new CreateNotificationCommand(buyerNotification), ct);
                        }

                        foreach (var sellerId in sellerDebits.Keys)
                        {
                            var sellerNotification = new CreateNotificationDto(
                                "کسورات لغو سفارش",
                                $"مبلغ {sellerDebits[sellerId]} از کیف‌پول شما بابت لغو سفارش {invoice.InvoiceNumber} کسر شد.",
                                NotificationType.System,
                                NotificationPriority.High,
                                null,
                                new Application.DTOs.Notifications.NotificationFilterDto(new[] { sellerId }));

                            await _mediator.Send(new CreateNotificationCommand(sellerNotification), ct);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send notifications for cancelled invoice {InvoiceId}", invoice.Id);
                    }

                    _logger.LogInformation("Invoice {InvoiceId} cancelled successfully", invoice.Id);

                    return Result<bool>.Success(true);
                },
                cancellationToken: cancellationToken);

            return result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(result.Error);
        }
    }
    */
}
