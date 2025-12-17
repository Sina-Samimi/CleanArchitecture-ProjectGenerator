using System;
using System.Diagnostics.CodeAnalysis;
using TestAttarClone.Domain.Base;
using TestAttarClone.Domain.Enums;
using TestAttarClone.Domain.Exceptions;

namespace TestAttarClone.Domain.Entities.Billing;

public sealed class WithdrawalRequest : Entity
{
    public string? SellerId { get; private set; }
    
    public string? UserId { get; private set; }
    
    public WithdrawalRequestType RequestType { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = null!;

    public string? BankAccountNumber { get; private set; }
    
    public string? CardNumber { get; private set; }
    
    public string? Iban { get; private set; }

    public string? BankName { get; private set; }

    public string? AccountHolderName { get; private set; }

    public string? Description { get; private set; }

    public string? AdminNotes { get; private set; }

    public WithdrawalRequestStatus Status { get; private set; }

    public string? ProcessedByUserId { get; private set; }

    public Domain.Entities.ApplicationUser? ProcessedByUser { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public Guid? WalletTransactionId { get; private set; }

    public WalletTransaction? WalletTransaction { get; private set; }

    [SetsRequiredMembers]
    private WithdrawalRequest()
    {
        Currency = "IRT";
        RequestType = WithdrawalRequestType.SellerRevenue;
    }

    [SetsRequiredMembers]
    public WithdrawalRequest(
        WithdrawalRequestType requestType,
        string? sellerId,
        string? userId,
        decimal amount,
        string currency,
        string? bankAccountNumber,
        string? cardNumber,
        string? iban,
        string? bankName,
        string? accountHolderName,
        string? description)
    {
        if (requestType == WithdrawalRequestType.SellerRevenue && string.IsNullOrWhiteSpace(sellerId))
        {
            throw new DomainException("برای درخواست برداشت سهم فروشنده، شناسه فروشنده الزامی است.");
        }
        
        if (requestType == WithdrawalRequestType.Wallet && string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("برای درخواست برداشت از کیف پول، شناسه کاربر الزامی است.");
        }
        
        RequestType = requestType;
        SellerId = NormalizeOptional(sellerId);
        UserId = NormalizeOptional(userId);
        SetAmount(amount);
        SetCurrency(currency);
        SetBankInfo(bankAccountNumber, cardNumber, iban, bankName, accountHolderName);
        SetDescription(description);
        Status = WithdrawalRequestStatus.Pending;
    }

    public void SetSellerId(string? sellerId)
    {
        if (RequestType == WithdrawalRequestType.SellerRevenue && string.IsNullOrWhiteSpace(sellerId))
        {
            throw new DomainException("برای درخواست برداشت سهم فروشنده، شناسه فروشنده الزامی است.");
        }

        SellerId = NormalizeOptional(sellerId);
        UpdateDate = DateTimeOffset.UtcNow;
    }
    
    public void SetUserId(string? userId)
    {
        if (RequestType == WithdrawalRequestType.Wallet && string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("برای درخواست برداشت از کیف پول، شناسه کاربر الزامی است.");
        }

        UserId = NormalizeOptional(userId);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("مبلغ برداشت باید بیشتر از صفر باشد.");
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("واحد پول الزامی است.");
        }

        Currency = currency.Trim().ToUpperInvariant();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetBankInfo(string? bankAccountNumber, string? cardNumber, string? iban, string? bankName, string? accountHolderName)
    {
        BankAccountNumber = NormalizeOptional(bankAccountNumber);
        CardNumber = NormalizeOptional(cardNumber);
        Iban = NormalizeOptional(iban);
        BankName = NormalizeOptional(bankName);
        AccountHolderName = NormalizeOptional(accountHolderName);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetDescription(string? description)
    {
        Description = NormalizeOptional(description);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Approve(string? adminNotes)
    {
        if (Status != WithdrawalRequestStatus.Pending)
        {
            throw new DomainException("فقط درخواست‌های در انتظار بررسی قابل تایید هستند.");
        }

        Status = WithdrawalRequestStatus.Approved;
        AdminNotes = NormalizeOptional(adminNotes);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Reject(string? adminNotes)
    {
        if (Status != WithdrawalRequestStatus.Pending && Status != WithdrawalRequestStatus.Approved)
        {
            throw new DomainException("فقط درخواست‌های در انتظار بررسی یا تایید شده قابل رد هستند.");
        }

        Status = WithdrawalRequestStatus.Rejected;
        AdminNotes = NormalizeOptional(adminNotes);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Process(string processedByUserId, Guid? walletTransactionId = null)
    {
        if (Status != WithdrawalRequestStatus.Approved)
        {
            throw new DomainException("فقط درخواست‌های تایید شده قابل پردازش هستند.");
        }

        if (string.IsNullOrWhiteSpace(processedByUserId))
        {
            throw new DomainException("شناسه کاربر پردازش‌کننده معتبر نیست.");
        }

        // For Wallet type, walletTransactionId is required
        if (RequestType == WithdrawalRequestType.Wallet && (!walletTransactionId.HasValue || walletTransactionId.Value == Guid.Empty))
        {
            throw new DomainException("برای درخواست برداشت از کیف پول، شناسه تراکنش کیف پول الزامی است.");
        }

        Status = WithdrawalRequestStatus.Processed;
        ProcessedByUserId = processedByUserId.Trim();
        ProcessedAt = DateTimeOffset.UtcNow;
        WalletTransactionId = walletTransactionId;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == WithdrawalRequestStatus.Processed)
        {
            throw new DomainException("درخواست‌های پرداخت شده قابل لغو نیستند.");
        }

        if (Status == WithdrawalRequestStatus.Cancelled)
        {
            return;
        }

        Status = WithdrawalRequestStatus.Cancelled;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

