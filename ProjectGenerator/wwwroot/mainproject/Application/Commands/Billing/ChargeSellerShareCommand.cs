using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.Application.Queries.Admin.FinancialSettings;
using MobiRooz.Domain.Enums;
using MobiRooz.Domain.Exceptions;
using MobiRooz.SharedKernel.BaseTypes;
using MobiRooz.SharedKernel.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MobiRooz.Application.Commands.Billing;

public sealed record ChargeSellerShareCommand(Guid InvoiceId) : ICommand;

public sealed class ChargeSellerShareCommandHandler : ICommandHandler<ChargeSellerShareCommand>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IProductRepository _productRepository;
    private readonly IFinancialSettingRepository _financialSettingRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ISellerProfileRepository _sellerProfileRepository;
    private readonly IMediator _mediator;
    private readonly IAuditContext _auditContext;
    private readonly ILogger<ChargeSellerShareCommandHandler> _logger;

    public ChargeSellerShareCommandHandler(
        IInvoiceRepository invoiceRepository,
        IProductRepository productRepository,
        IFinancialSettingRepository financialSettingRepository,
        IWalletRepository walletRepository,
        ISellerProfileRepository sellerProfileRepository,
        IMediator mediator,
        IAuditContext auditContext,
        ILogger<ChargeSellerShareCommandHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _productRepository = productRepository;
        _financialSettingRepository = financialSettingRepository;
        _walletRepository = walletRepository;
        _sellerProfileRepository = sellerProfileRepository;
        _mediator = mediator;
        _auditContext = auditContext;
        _logger = logger;
    }

    public async Task<Result> Handle(ChargeSellerShareCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ChargeSellerShareCommand for invoice {InvoiceId}", request.InvoiceId);

        if (request.InvoiceId == Guid.Empty)
        {
            _logger.LogWarning("Invalid invoice ID in ChargeSellerShareCommand");
            return Result.Failure("شناسه فاکتور معتبر نیست.");
        }

        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogWarning("Invoice {InvoiceId} not found in ChargeSellerShareCommand", request.InvoiceId);
            return Result.Failure("فاکتور مورد نظر یافت نشد.");
        }

        // Only charge seller share if invoice is fully paid
        if (invoice.Status != InvoiceStatus.Paid)
        {
            _logger.LogInformation("Invoice {InvoiceId} is not fully paid (Status: {Status}), skipping seller share charge", 
                request.InvoiceId, invoice.Status);
            return Result.Success(); // Not an error, just skip
        }

        // Get financial settings (default values)
        var financialSettingsResult = await _mediator.Send(new GetFinancialSettingsQuery(), cancellationToken);
        if (!financialSettingsResult.IsSuccess || financialSettingsResult.Value is null)
        {
            _logger.LogError("Failed to get financial settings in ChargeSellerShareCommand: {Error}", 
                financialSettingsResult.Error);
            return Result.Failure("خطا در دریافت تنظیمات مالی.");
        }

        var financialSettings = financialSettingsResult.Value;
        var defaultSellerSharePercentage = financialSettings.SellerProductSharePercentage;
        var platformCommissionPercentage = financialSettings.PlatformCommissionPercentage;
        var calculationMethod = financialSettings.CommissionCalculationMethod;
        
        _logger.LogInformation("=== Financial Settings Retrieved ===");
        _logger.LogInformation("Default seller share percentage: {Percentage}%", defaultSellerSharePercentage);
        _logger.LogInformation("Platform commission percentage: {PlatformPercentage}%", platformCommissionPercentage);
        _logger.LogInformation("Calculation method: {Method}", calculationMethod);
        _logger.LogInformation("====================================");
        
        if (defaultSellerSharePercentage <= 0)
        {
            _logger.LogWarning("Default seller share percentage is 0 or negative ({Percentage}%), will use seller-specific percentage if available", defaultSellerSharePercentage);
        }

        // Get product items from invoice
        var productItems = invoice.Items
            .Where(item => item.ItemType == InvoiceItemType.Product && item.ReferenceId.HasValue)
            .ToList();

        if (productItems.Count == 0)
        {
            return Result.Success(); // No products in invoice
        }

        // Get product IDs
        var productIds = productItems
            .Select(item => item.ReferenceId!.Value)
            .Distinct()
            .ToList();

        // Get products to find seller IDs
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productMap = products.ToDictionary(p => p.Id);

        // Group invoice items by seller ID
        var sellerItems = new Dictionary<string, List<(Domain.Entities.Billing.InvoiceItem Item, Domain.Entities.Catalog.Product Product)>>();

        foreach (var item in productItems)
        {
            if (!item.ReferenceId.HasValue || !productMap.TryGetValue(item.ReferenceId.Value, out var product))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(product.SellerId))
            {
                continue;
            }

            var sellerId = product.SellerId;
            if (!sellerItems.ContainsKey(sellerId))
            {
                sellerItems[sellerId] = new List<(Domain.Entities.Billing.InvoiceItem, Domain.Entities.Catalog.Product)>();
            }

            sellerItems[sellerId].Add((item, product));
        }

        if (sellerItems.Count == 0)
        {
            return Result.Success(); // No sellers found
        }

        var audit = _auditContext.Capture();

        // Charge each seller's wallet
        foreach (var (sellerId, items) in sellerItems)
        {
            try
            {
                // Calculate seller share for this seller's items
                decimal totalAmount = 0;
                foreach (var (item, _) in items)
                {
                    totalAmount += item.Total;
                }

                // Get seller-specific share percentage, or use default from financial settings
                var sellerProfile = await _sellerProfileRepository.GetByUserIdAsync(sellerId, cancellationToken);
                var sellerSharePercentage = sellerProfile?.SellerSharePercentage ?? defaultSellerSharePercentage;

                if (sellerSharePercentage <= 0)
                {
                    _logger.LogWarning("Seller share percentage is 0 or negative ({Percentage}%) for seller {SellerId}, skipping charge", 
                        sellerSharePercentage, sellerId);
                    continue; // Skip this seller
                }

                _logger.LogInformation("=== Calculating Seller Share for Seller {SellerId} ===", sellerId);
                _logger.LogInformation("Total amount from invoice items: {TotalAmount}", totalAmount);
                _logger.LogInformation("Seller share percentage: {Percentage}% {Source}", 
                    sellerSharePercentage, 
                    sellerProfile?.SellerSharePercentage.HasValue == true ? "(seller-specific)" : "(from financial settings)");
                _logger.LogInformation("Platform commission percentage: {PlatformPercentage}%", platformCommissionPercentage);
                _logger.LogInformation("Calculation method: {Method}", calculationMethod);

                // Calculate seller share based on calculation method
                decimal sellerShare;
                if (calculationMethod == Domain.Enums.PlatformCommissionCalculationMethod.DeductFromSeller)
                {
                    // Deduct platform commission from seller share
                    var baseSellerShare = totalAmount * sellerSharePercentage / 100m;
                    var platformCommission = totalAmount * platformCommissionPercentage / 100m;
                    sellerShare = decimal.Round(
                        baseSellerShare - platformCommission,
                        2,
                        MidpointRounding.AwayFromZero);
                    
                    _logger.LogInformation("Using DeductFromSeller method:");
                    _logger.LogInformation("  - Base seller share ({Percentage}%): {BaseShare}", sellerSharePercentage, baseSellerShare);
                    _logger.LogInformation("  - Platform commission ({PlatformPercentage}%): {Commission}", platformCommissionPercentage, platformCommission);
                    _logger.LogInformation("  - Final seller share (Base - Commission): {SellerShare}", sellerShare);
                }
                else // Complementary
                {
                    // Seller share is calculated directly, platform commission is the remainder
                    sellerShare = decimal.Round(
                        totalAmount * sellerSharePercentage / 100m,
                        2,
                        MidpointRounding.AwayFromZero);
                    
                    var platformCommission = totalAmount - sellerShare;
                    _logger.LogInformation("Using Complementary method:");
                    _logger.LogInformation("  - Seller share ({Percentage}%): {SellerShare}", sellerSharePercentage, sellerShare);
                    _logger.LogInformation("  - Platform commission (remainder): {PlatformCommission}", platformCommission);
                }

                _logger.LogInformation("=== Final Result ===");
                _logger.LogInformation("Seller ID: {SellerId}", sellerId);
                _logger.LogInformation("Total amount: {TotalAmount}", totalAmount);
                _logger.LogInformation("Seller share to credit: {SellerShare}", sellerShare);
                _logger.LogInformation("Percentage of total: {Percentage}%", totalAmount > 0 ? (sellerShare / totalAmount * 100m) : 0m);
                _logger.LogInformation("================================");

                if (sellerShare <= 0)
                {
                    _logger.LogWarning("Seller share is zero or negative for seller {SellerId}, skipping", sellerId);
                    continue; // Skip if share is zero or negative
                }

                // Get or create seller wallet (with transactions to check for duplicates)
                var wallet = await _walletRepository.GetByUserIdWithTransactionsAsync(sellerId, null, cancellationToken);
                
                // Define metadata for checking duplicates and creating transaction
                var metadata = $"SELLER_SHARE_{sellerId}_INV_{invoice.Id}";
                
                // Check if already charged (by checking wallet transactions with metadata)
                if (wallet is not null)
                {
                    var existingTransaction = wallet.Transactions
                        .FirstOrDefault(t =>
                            t.Type == Domain.Enums.WalletTransactionType.Credit &&
                            t.Status == TransactionStatus.Succeeded &&
                            t.InvoiceId == invoice.Id &&
                            !string.IsNullOrWhiteSpace(t.Metadata) &&
                            t.Metadata.Contains(metadata, StringComparison.OrdinalIgnoreCase));

                    if (existingTransaction is not null)
                    {
                        continue; // Already charged
                    }
                }

                // Get or create seller wallet
                var isNewWallet = wallet is null;
                wallet = wallet ?? new Domain.Entities.Billing.WalletAccount(sellerId, invoice.Currency)
                {
                    CreatorId = audit.UserId,
                    CreateDate = audit.Timestamp,
                    UpdateDate = audit.Timestamp,
                    Ip = audit.IpAddress,
                    UpdaterId = audit.UserId
                };

                if (!isNewWallet && !string.Equals(wallet.Currency, invoice.Currency, StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Currency mismatch, skip this seller
                }

                // Credit seller wallet
                var reference = GenerateReference("SELLER");
                var description = $"سهم فروشنده از فاکتور {invoice.InvoiceNumber}";

                _logger.LogInformation("Crediting seller {SellerId} wallet with amount {Amount} for invoice {InvoiceId}", 
                    sellerId, sellerShare, invoice.Id);

                var walletTransaction = wallet.Credit(
                    sellerShare,
                    reference,
                    description,
                    metadata: metadata,
                    invoiceId: invoice.Id,
                    paymentTransactionId: null,
                    TransactionStatus.Succeeded,
                    audit.Timestamp);

                if (isNewWallet)
                {
                    await _walletRepository.AddAsync(wallet, cancellationToken);
                }
                else
                {
                    await _walletRepository.UpdateAsync(wallet, cancellationToken);
                }

                _logger.LogInformation("Successfully credited seller {SellerId} wallet with amount {Amount} for invoice {InvoiceId}", 
                    sellerId, sellerShare, invoice.Id);
            }
            catch (DomainException ex)
            {
                _logger.LogError(ex, "Domain exception while charging seller {SellerId} share for invoice {InvoiceId}", 
                    sellerId, invoice.Id);
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while charging seller {SellerId} share for invoice {InvoiceId}", 
                    sellerId, invoice.Id);
                continue;
            }
        }

        _logger.LogInformation("ChargeSellerShareCommand completed for invoice {InvoiceId}", request.InvoiceId);
        return Result.Success();
    }

    private static string GenerateReference(string prefix)
    {
        return ReferenceGenerator.GenerateReadableReference($"WL{prefix}", DateTimeOffset.UtcNow);
    }
}

