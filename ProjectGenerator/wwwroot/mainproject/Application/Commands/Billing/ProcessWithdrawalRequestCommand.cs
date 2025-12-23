using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Billing;
using MobiRooz.Application.Interfaces;
using MobiRooz.Application.Queries.Billing;
using MobiRooz.Domain.Entities.Billing;
using MobiRooz.Domain.Enums;
using MobiRooz.Domain.Exceptions;
using MobiRooz.SharedKernel.BaseTypes;
using MobiRooz.SharedKernel.Helpers;
using MediatR;

namespace MobiRooz.Application.Commands.Billing;

public sealed record ProcessWithdrawalRequestCommand(
    Guid RequestId,
    string AdminUserId) : ICommand<WithdrawalRequestDetailsDto>;

public sealed class ProcessWithdrawalRequestCommandHandler : ICommandHandler<ProcessWithdrawalRequestCommand, WithdrawalRequestDetailsDto>
{
    private readonly IWithdrawalRequestRepository _withdrawalRequestRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IAuditContext _auditContext;
    private readonly IMediator _mediator;
    private readonly IPaymentSettingRepository _paymentSettingRepository;
    private readonly IInvoiceRepository _invoiceRepository;

    public ProcessWithdrawalRequestCommandHandler(
        IWithdrawalRequestRepository withdrawalRequestRepository,
        IWalletRepository walletRepository,
        IAuditContext auditContext,
        IMediator mediator,
        IPaymentSettingRepository paymentSettingRepository,
        IInvoiceRepository invoiceRepository)
    {
        _withdrawalRequestRepository = withdrawalRequestRepository;
        _walletRepository = walletRepository;
        _auditContext = auditContext;
        _mediator = mediator;
        _paymentSettingRepository = paymentSettingRepository;
        _invoiceRepository = invoiceRepository;
    }

    public async Task<Result<WithdrawalRequestDetailsDto>> Handle(ProcessWithdrawalRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AdminUserId))
        {
            return Result<WithdrawalRequestDetailsDto>.Failure("شناسه کاربر ادمین معتبر نیست.");
        }

        try
        {
            var withdrawalRequest = await _withdrawalRequestRepository.GetByIdForUpdateAsync(request.RequestId, cancellationToken);
            if (withdrawalRequest is null)
            {
                return Result<WithdrawalRequestDetailsDto>.Failure("درخواست برداشت یافت نشد.");
            }

            if (withdrawalRequest.Status != WithdrawalRequestStatus.Approved)
            {
                return Result<WithdrawalRequestDetailsDto>.Failure("فقط درخواست‌های تایید شده قابل پردازش هستند.");
            }

            // Different validation and processing based on request type
            var audit = _auditContext.Capture();

            if (withdrawalRequest.RequestType == WithdrawalRequestType.SellerRevenue)
            {
                // For seller revenue withdrawal: check total revenue and total withdrawals
                // This type of withdrawal does NOT deduct from wallet balance
                // It only tracks the withdrawal from total revenue
                if (string.IsNullOrWhiteSpace(withdrawalRequest.SellerId))
                {
                    return Result<WithdrawalRequestDetailsDto>.Failure("شناسه فروشنده معتبر نیست.");
                }

                var totalRevenueQuery = new Application.Queries.Sellers.GetSellerPaymentsQuery(withdrawalRequest.SellerId);
                var totalRevenueResult = await _mediator.Send(totalRevenueQuery, cancellationToken);
                
                if (!totalRevenueResult.IsSuccess || totalRevenueResult.Value is null)
                {
                    return Result<WithdrawalRequestDetailsDto>.Failure("خطا در دریافت اطلاعات درآمد.");
                }

                var totalWithdrawalsQuery = new GetSellerTotalWithdrawalsQuery(withdrawalRequest.SellerId);
                var totalWithdrawalsResult = await _mediator.Send(totalWithdrawalsQuery, cancellationToken);
                
                if (!totalWithdrawalsResult.IsSuccess)
                {
                    return Result<WithdrawalRequestDetailsDto>.Failure("خطا در محاسبه کل برداشت‌های انجام شده.");
                }

                var totalRevenue = totalRevenueResult.Value.TotalRevenue;
                var totalWithdrawn = totalWithdrawalsResult.Value;
                var availableAmount = totalRevenue - totalWithdrawn;

                if (availableAmount < withdrawalRequest.Amount)
                {
                    return Result<WithdrawalRequestDetailsDto>.Failure(
                        $"مبلغ درخواست شده ({withdrawalRequest.Amount:N0} {withdrawalRequest.Currency}) بیشتر از مبلغ قابل برداشت ({availableAmount:N0} {withdrawalRequest.Currency}) است. " +
                        $"درآمد کل: {totalRevenue:N0} {withdrawalRequest.Currency}، " +
                        $"برداشت شده: {totalWithdrawn:N0} {withdrawalRequest.Currency}");
                }

                // Check if payment gateway is active
                var paymentSetting = await _paymentSettingRepository.GetCurrentAsync(cancellationToken);
                var isGatewayActive = paymentSetting?.IsActive == true && 
                                      !string.IsNullOrWhiteSpace(paymentSetting.ZarinPalMerchantId);

                if (isGatewayActive)
                {
                    // Create invoice for withdrawal payment via gateway
                    var invoiceNumber = Invoice.GenerateInvoiceNumber();
                    var invoiceExists = await _invoiceRepository.ExistsByNumberAsync(invoiceNumber, null, cancellationToken);
                    if (invoiceExists)
                    {
                        // Retry with new number if exists
                        invoiceNumber = Invoice.GenerateInvoiceNumber();
                    }

                    var invoiceItems = new List<CreateInvoiceCommand.Item>
                    {
                        new CreateInvoiceCommand.Item(
                            Name: $"برداشت سهم فروشنده - درخواست #{withdrawalRequest.Id}",
                            Description: $"برداشت مبلغ {withdrawalRequest.Amount:N0} {withdrawalRequest.Currency} از درآمد فروشنده",
                            ItemType: InvoiceItemType.Miscellaneous,
                            ReferenceId: withdrawalRequest.Id,
                            Quantity: 1,
                            UnitPrice: withdrawalRequest.Amount,
                            DiscountAmount: null,
                            Attributes: new List<CreateInvoiceCommand.Attribute>
                            {
                                new CreateInvoiceCommand.Attribute("WithdrawalRequestId", withdrawalRequest.Id.ToString()),
                                new CreateInvoiceCommand.Attribute("RequestType", "SellerRevenue")
                            },
                            VariantId: null)
                    };

                    var createInvoiceCommand = new CreateInvoiceCommand(
                        InvoiceNumber: invoiceNumber,
                        Title: $"فاکتور برداشت سهم فروشنده - درخواست #{withdrawalRequest.Id}",
                        Description: $"فاکتور پرداخت برداشت سهم فروشنده به مبلغ {withdrawalRequest.Amount:N0} {withdrawalRequest.Currency}",
                        Currency: withdrawalRequest.Currency,
                        UserId: null, // No user ID for seller revenue withdrawal
                        IssueDate: audit.Timestamp,
                        DueDate: null,
                        TaxAmount: 0,
                        AdjustmentAmount: 0,
                        ExternalReference: $"WITHDRAWAL_REQUEST:{withdrawalRequest.Id}",
                        Items: invoiceItems);

                    var createInvoiceResult = await _mediator.Send(createInvoiceCommand, cancellationToken);
                    if (!createInvoiceResult.IsSuccess)
                    {
                        return Result<WithdrawalRequestDetailsDto>.Failure(
                            $"خطا در ایجاد فاکتور پرداخت: {createInvoiceResult.Error}");
                    }

                    var invoiceId = createInvoiceResult.Value;

                    // Create a pending payment transaction for gateway payment
                    var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
                    if (invoice is null)
                    {
                        return Result<WithdrawalRequestDetailsDto>.Failure("فاکتور ایجاد شده یافت نشد.");
                    }

                    var reference = ReferenceGenerator.GenerateReadableReference("WDR", DateTimeOffset.UtcNow);
                    var paymentTransaction = invoice.AddTransaction(
                        withdrawalRequest.Amount,
                        PaymentMethod.OnlineGateway,
                        TransactionStatus.Pending,
                        reference,
                        "ZarinPal",
                        $"پرداخت برداشت سهم فروشنده - درخواست #{withdrawalRequest.Id}",
                        $"WITHDRAWAL_REQUEST:{withdrawalRequest.Id}");

                    await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

                    // Don't process the withdrawal request yet - it will be processed after payment verification
                    // Just mark it as approved and wait for payment
                    // The withdrawal will be processed when payment is verified
                    // Return the invoice ID in the result so admin can redirect to payment
                    await _withdrawalRequestRepository.UpdateAsync(withdrawalRequest, cancellationToken);
                    
                    // Reload to get updated data
                    withdrawalRequest = await _withdrawalRequestRepository.GetByIdAsync(request.RequestId, cancellationToken);
                    var resultDto = withdrawalRequest!.ToDetailsDto(invoiceId);
                    return Result<WithdrawalRequestDetailsDto>.Success(resultDto);
                }
                else
                {
                    // Payment gateway is not active, process directly without invoice
                    // For seller revenue: No wallet transaction needed, just process the request
                    // The withdrawal is tracked by the request status, not by wallet balance
                    withdrawalRequest.Process(request.AdminUserId, null);
                }
            }
            else if (withdrawalRequest.RequestType == WithdrawalRequestType.Wallet)
            {
                // For wallet withdrawal: check wallet balance and deduct from wallet
                if (string.IsNullOrWhiteSpace(withdrawalRequest.UserId))
                {
                    return Result<WithdrawalRequestDetailsDto>.Failure("شناسه کاربر معتبر نیست.");
                }

                var wallet = await _walletRepository.GetByUserIdAsync(withdrawalRequest.UserId, cancellationToken);
                if (wallet is null)
                {
                    return Result<WithdrawalRequestDetailsDto>.Failure("کیف پول یافت نشد.");
                }

                if (wallet.IsLocked)
                {
                    return Result<WithdrawalRequestDetailsDto>.Failure("کیف پول در حالت قفل است.");
                }

                if (wallet.Balance < withdrawalRequest.Amount)
                {
                    return Result<WithdrawalRequestDetailsDto>.Failure(
                        $"موجودی کیف پول ({wallet.Balance:N0} {withdrawalRequest.Currency}) برای این درخواست کافی نیست.");
                }

                // Create wallet transaction (debit) for wallet withdrawal
                var reference = ReferenceGenerator.GenerateReadableReference("WDR", DateTimeOffset.UtcNow);
                var walletTransaction = wallet.Debit(
                    withdrawalRequest.Amount,
                    reference,
                    $"برداشت از کیف پول - درخواست #{withdrawalRequest.Id}",
                    metadata: $"WITHDRAWAL_REQUEST:{withdrawalRequest.Id}",
                    invoiceId: null,
                    paymentTransactionId: null,
                    TransactionStatus.Succeeded,
                    audit.Timestamp);

                // Update wallet
                await _walletRepository.UpdateAsync(wallet, cancellationToken);

                // Process the withdrawal request with wallet transaction
                withdrawalRequest.Process(request.AdminUserId, walletTransaction.Id);
            }
            else
            {
                return Result<WithdrawalRequestDetailsDto>.Failure("نوع درخواست برداشت معتبر نیست.");
            }

            await _withdrawalRequestRepository.UpdateAsync(withdrawalRequest, cancellationToken);

            // Reload to get wallet transaction
            withdrawalRequest = await _withdrawalRequestRepository.GetByIdAsync(request.RequestId, cancellationToken);

            return Result<WithdrawalRequestDetailsDto>.Success(withdrawalRequest!.ToDetailsDto(null));
        }
        catch (DomainException ex)
        {
            return Result<WithdrawalRequestDetailsDto>.Failure(ex.Message);
        }
    }
}

