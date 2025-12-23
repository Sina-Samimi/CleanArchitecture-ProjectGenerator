using System;
using System.Diagnostics.CodeAnalysis;
using MobiRooz.Domain.Base;
using MobiRooz.Domain.Enums;
using MobiRooz.Domain.Exceptions;

namespace MobiRooz.Domain.Entities.Billing;

public sealed class WalletTransaction : Entity
{
    public Guid WalletAccountId { get; private set; }

    public WalletAccount WalletAccount { get; private set; } = null!;

    public decimal Amount { get; private set; }

    public WalletTransactionType Type { get; private set; }

    public TransactionStatus Status { get; private set; }

    public decimal BalanceAfterTransaction { get; private set; }

    public string Reference { get; private set; } = null!;

    public string? Description { get; private set; }

    public string? Metadata { get; private set; }

    public Guid? InvoiceId { get; private set; }

    public Guid? PaymentTransactionId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    [SetsRequiredMembers]
    private WalletTransaction()
    {
        Reference = string.Empty;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    [SetsRequiredMembers]
    internal WalletTransaction(
        WalletAccount account,
        decimal amount,
        WalletTransactionType type,
        TransactionStatus status,
        decimal balanceAfterTransaction,
        string reference,
        string? description,
        string? metadata,
        Guid? invoiceId,
        Guid? paymentTransactionId,
        DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(account);
        WalletAccount = account;
        WalletAccountId = account.Id;
        SetAmount(amount);
        SetType(type);
        SetStatus(status);
        SetReference(reference);
        SetDescription(description);
        SetMetadata(metadata);
        BalanceAfterTransaction = decimal.Round(balanceAfterTransaction, 2, MidpointRounding.AwayFromZero);
        InvoiceId = invoiceId;
        PaymentTransactionId = paymentTransactionId;
        OccurredAt = occurredAt;
    }

    public void AttachPayment(Guid paymentTransactionId)
    {
        if (paymentTransactionId == Guid.Empty)
        {
            throw new DomainException("شناسه تراکنش پرداخت نامعتبر است.");
        }

        PaymentTransactionId = paymentTransactionId;
    }

    private void SetAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("مبلغ تراکنش کیف پول باید بیشتر از صفر باشد.");
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    private void SetType(WalletTransactionType type)
    {
        if (!Enum.IsDefined(typeof(WalletTransactionType), type))
        {
            throw new DomainException("نوع تراکنش کیف پول نامعتبر است.");
        }

        Type = type;
    }

    private void SetStatus(TransactionStatus status)
    {
        if (!Enum.IsDefined(typeof(TransactionStatus), status))
        {
            throw new DomainException("وضعیت تراکنش کیف پول نامعتبر است.");
        }

        Status = status;
    }

    private void SetReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new DomainException("شناسه مرجع تراکنش مشخص نشده است.");
        }

        Reference = reference.Trim();
    }

    private void SetDescription(string? description)
        => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private void SetMetadata(string? metadata)
        => Metadata = string.IsNullOrWhiteSpace(metadata) ? null : metadata.Trim();
}
