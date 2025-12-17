using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using LogsDtoCloneTest.Domain.Base;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.Domain.Exceptions;
using LogsDtoCloneTest.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LogsDtoCloneTest.Domain.Entities.Billing;

public sealed class WalletAccount : Entity, IAggregateRoot
{
    private readonly List<WalletTransaction> _transactions = new();
    private IReadOnlyCollection<WalletTransaction>? _transactionsSnapshot;

    public string UserId { get; private set; } = null!;

    public string Currency { get; private set; } = null!;

    public decimal Balance { get; private set; }

    public bool IsLocked { get; private set; }

    public DateTimeOffset LastActivityOn { get; private set; }

    [BackingField(nameof(_transactions))]
    public IReadOnlyCollection<WalletTransaction> Transactions =>
        _transactionsSnapshot ??= new ReadOnlyCollection<WalletTransaction>(_transactions);

    internal ICollection<WalletTransaction> TransactionsCollection => _transactions;

    [SetsRequiredMembers]
    private WalletAccount()
    {
        UserId = string.Empty;
        Currency = "IRT";
        LastActivityOn = DateTimeOffset.UtcNow;
    }

    [SetsRequiredMembers]
    public WalletAccount(string userId, string currency)
    {
        SetUser(userId);
        SetCurrency(currency);
        Balance = 0m;
        IsLocked = false;
        LastActivityOn = DateTimeOffset.UtcNow;
    }

    public void SetUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("شناسه کاربر برای کیف پول الزامی است.");
        }

        UserId = userId.Trim();
        Touch();
    }

    public void SetCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("واحد پول کیف پول مشخص نشده است.");
        }

        Currency = currency.Trim().ToUpperInvariant();
        Touch();
    }

    public void Lock()
    {
        if (IsLocked)
        {
            return;
        }

        IsLocked = true;
        Touch();
    }

    public void Unlock()
    {
        if (!IsLocked)
        {
            return;
        }

        IsLocked = false;
        Touch();
    }

    public WalletTransaction Credit(
        decimal amount,
        string reference,
        string? description,
        string? metadata,
        Guid? invoiceId,
        Guid? paymentTransactionId,
        TransactionStatus status,
        DateTimeOffset? occurredAt = null)
    {
        EnsureUnlocked();

        if (amount <= 0)
        {
            throw new DomainException("مبلغ واریز باید بیشتر از صفر باشد.");
        }

        var normalizedReference = NormalizeReference(reference);
        var timestamp = occurredAt ?? DateTimeOffset.UtcNow;

        var newBalance = Balance;
        if (status == TransactionStatus.Succeeded)
        {
            newBalance = RoundMoney(Balance + amount);
        }

        var transaction = AddTransaction(
            WalletTransactionType.Credit,
            amount,
            status,
            newBalance,
            normalizedReference,
            description,
            metadata,
            invoiceId,
            paymentTransactionId,
            timestamp);

        if (status == TransactionStatus.Succeeded)
        {
            Balance = newBalance;
        }

        return transaction;
    }

    public WalletTransaction Debit(
        decimal amount,
        string reference,
        string? description,
        string? metadata,
        Guid? invoiceId,
        Guid? paymentTransactionId,
        TransactionStatus status,
        DateTimeOffset? occurredAt = null)
    {
        EnsureUnlocked();

        if (amount <= 0)
        {
            throw new DomainException("مبلغ برداشت باید بیشتر از صفر باشد.");
        }

        var normalizedReference = NormalizeReference(reference);
        var timestamp = occurredAt ?? DateTimeOffset.UtcNow;

        var newBalance = Balance;
        if (status == TransactionStatus.Succeeded)
        {
            newBalance = RoundMoney(Balance - amount);
            if (newBalance < 0)
            {
                throw new DomainException("موجودی کیف پول برای انجام این تراکنش کافی نیست.");
            }
        }

        var transaction = AddTransaction(
            WalletTransactionType.Debit,
            amount,
            status,
            newBalance,
            normalizedReference,
            description,
            metadata,
            invoiceId,
            paymentTransactionId,
            timestamp);

        if (status == TransactionStatus.Succeeded)
        {
            Balance = newBalance;
        }

        return transaction;
    }

    private WalletTransaction AddTransaction(
        WalletTransactionType type,
        decimal amount,
        TransactionStatus status,
        decimal balanceAfter,
        string reference,
        string? description,
        string? metadata,
        Guid? invoiceId,
        Guid? paymentTransactionId,
        DateTimeOffset occurredAt)
    {
        var transaction = new WalletTransaction(
            this,
            amount,
            type,
            status,
            balanceAfter,
            reference,
            description,
            metadata,
            invoiceId,
            paymentTransactionId,
            occurredAt);

        _transactions.Add(transaction);
        InvalidateTransactionsSnapshot();
        LastActivityOn = occurredAt;
        UpdateDate = occurredAt;
        return transaction;
    }

    private void InvalidateTransactionsSnapshot()
        => _transactionsSnapshot = null;

    private void EnsureUnlocked()
    {
        if (IsLocked)
        {
            throw new DomainException("کیف پول در حالت قفل است.");
        }
    }

    private static string NormalizeReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new DomainException("شناسه مرجع تراکنش الزامی است.");
        }

        return reference.Trim();
    }

    private static decimal RoundMoney(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private void Touch()
    {
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
