using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TestAttarClone.Domain.Base;
using TestAttarClone.Domain.Enums;
using TestAttarClone.Domain.Exceptions;
using TestAttarClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace TestAttarClone.Domain.Entities.Billing;

public sealed class Invoice : Entity, IAggregateRoot
{
    private readonly List<InvoiceItem> _items = new();
    private readonly List<PaymentTransaction> _transactions = new();
    private IReadOnlyCollection<InvoiceItem>? _itemsSnapshot;
    private IReadOnlyCollection<PaymentTransaction>? _transactionsSnapshot;

    public string InvoiceNumber { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public string? UserId { get; private set; }

    public string Currency { get; private set; } = null!;

    public InvoiceStatus Status { get; private set; }

    public DateTimeOffset IssueDate { get; private set; }

    public DateTimeOffset? DueDate { get; private set; }

    public decimal TaxAmount { get; private set; }

    public decimal AdjustmentAmount { get; private set; }

    public string? ExternalReference { get; private set; }

    public Guid? ShippingAddressId { get; private set; }

    public string? ShippingRecipientName { get; private set; }

    public string? ShippingRecipientPhone { get; private set; }

    public string? ShippingProvince { get; private set; }

    public string? ShippingCity { get; private set; }

    public string? ShippingPostalCode { get; private set; }

    public string? ShippingAddressLine { get; private set; }

    public string? ShippingPlaque { get; private set; }

    public string? ShippingUnit { get; private set; }

    [BackingField(nameof(_items))]
    public IReadOnlyCollection<InvoiceItem> Items => _itemsSnapshot ??= new ReadOnlyCollection<InvoiceItem>(_items);

    internal ICollection<InvoiceItem> ItemsCollection => _items;

    [BackingField(nameof(_transactions))]
    public IReadOnlyCollection<PaymentTransaction> Transactions => _transactionsSnapshot ??= new ReadOnlyCollection<PaymentTransaction>(_transactions);

    internal ICollection<PaymentTransaction> TransactionsCollection => _transactions;

    public decimal Subtotal => RoundMoney(_items.Sum(item => item.Subtotal));

    public decimal DiscountTotal => RoundMoney(_items.Sum(item => item.DiscountAmount ?? 0m));

    public decimal ItemsTotal => RoundMoney(Subtotal - DiscountTotal);

    public decimal GrandTotal
    {
        get
        {
            var total = ItemsTotal + TaxAmount + AdjustmentAmount;
            return total < 0 ? 0 : RoundMoney(total);
        }
    }

    public decimal PaidAmount => RoundMoney(_transactions
        .Where(transaction => transaction.Status == TransactionStatus.Succeeded)
        .Sum(transaction => transaction.Amount));

    public decimal OutstandingAmount
    {
        get
        {
            var outstanding = GrandTotal - PaidAmount;
            return outstanding <= 0 ? 0 : RoundMoney(outstanding);
        }
    }

    [SetsRequiredMembers]
    private Invoice()
    {
        InvoiceNumber = string.Empty;
        Title = string.Empty;
        Currency = "IRT";
        Status = InvoiceStatus.Draft;
        IssueDate = DateTimeOffset.Now;
    }

    [SetsRequiredMembers]
    public Invoice(
        string invoiceNumber,
        string title,
        string? description,
        string currency,
        string? userId,
        DateTimeOffset issueDate,
        DateTimeOffset? dueDate,
        decimal taxAmount,
        decimal adjustmentAmount,
        string? externalReference)
    {
        SetInvoiceNumber(invoiceNumber);
        SetTitle(title);
        SetDescription(description);
        SetCurrency(currency);
        SetUser(userId);
        IssueDate = issueDate;
        SetDueDate(dueDate);
        SetTaxAmount(taxAmount);
        SetAdjustmentAmount(adjustmentAmount);
        ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim();
        Status = InvoiceStatus.Pending;
        EvaluateStatus(null);
    }

    public static string GenerateInvoiceNumber()
    {
        var random = Guid.NewGuid().ToString("N").ToUpperInvariant();
        return $"INV-{random[..8]}";
    }

    public void SetInvoiceNumber(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            throw new DomainException("Invoice number cannot be empty.");
        }

        InvoiceNumber = invoiceNumber.Trim();
        Touch();
    }

    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Invoice title is required.");
        }

        Title = title.Trim();
        Touch();
    }

    public void SetDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Touch();
    }

    public void SetExternalReference(string? externalReference)
    {
        ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim();
        Touch();
    }

    public void SetShippingAddress(
        Guid? addressId,
        string? recipientName,
        string? recipientPhone,
        string? province,
        string? city,
        string? postalCode,
        string? addressLine,
        string? plaque = null,
        string? unit = null)
    {
        ShippingAddressId = addressId;
        ShippingRecipientName = string.IsNullOrWhiteSpace(recipientName) ? null : recipientName.Trim();
        ShippingRecipientPhone = string.IsNullOrWhiteSpace(recipientPhone) ? null : recipientPhone.Trim();
        ShippingProvince = string.IsNullOrWhiteSpace(province) ? null : province.Trim();
        ShippingCity = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        ShippingPostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();
        ShippingAddressLine = string.IsNullOrWhiteSpace(addressLine) ? null : addressLine.Trim();
        ShippingPlaque = string.IsNullOrWhiteSpace(plaque) ? null : plaque.Trim();
        ShippingUnit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        Touch();
    }

    public void SetCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Invoice currency is required.");
        }

        Currency = currency.Trim().ToUpperInvariant();
        Touch();
    }

    public void SetUser(string? userId)
    {
        UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
        Touch();
    }

    public void SetIssueDate(DateTimeOffset issueDate)
    {
        IssueDate = issueDate;

        if (DueDate is not null && DueDate.Value < IssueDate)
        {
            throw new DomainException("Due date cannot be before issue date.");
        }

        EvaluateStatus(null);
        Touch();
    }

    public void SetDueDate(DateTimeOffset? dueDate)
    {
        if (dueDate is not null && dueDate.Value < IssueDate)
        {
            throw new DomainException("Due date cannot be before issue date.");
        }

        DueDate = dueDate;
        EvaluateStatus(null);
        Touch();
    }

    public void SetTaxAmount(decimal taxAmount)
    {
        if (taxAmount < 0)
        {
            throw new DomainException("Tax amount cannot be negative.");
        }

        TaxAmount = RoundMoney(taxAmount);
        EvaluateStatus(null);
        Touch();
    }

    public void SetAdjustmentAmount(decimal adjustmentAmount)
    {
        AdjustmentAmount = RoundMoney(adjustmentAmount);
        EvaluateStatus(null);
        Touch();
    }

    public InvoiceItem AddItem(
        string name,
        string? description,
        InvoiceItemType itemType,
        Guid? referenceId,
        decimal quantity,
        decimal unitPrice,
        decimal? discountAmount,
        IReadOnlyCollection<(string Key, string Value)>? attributes,
        Guid? variantId = null)
    {
        var item = new InvoiceItem(this, name, description, itemType, referenceId, quantity, unitPrice, discountAmount, variantId);

        if (attributes is not null)
        {
            foreach (var attribute in attributes)
            {
                item.AddAttribute(attribute.Key, attribute.Value);
            }
        }

        _items.Add(item);
        InvalidateItemsSnapshot();
        EvaluateStatus(null);
        Touch();
        return item;
    }

    public void ResetItems(IEnumerable<(string Name, string? Description, InvoiceItemType Type, Guid? ReferenceId, decimal Quantity, decimal UnitPrice, decimal? DiscountAmount, IReadOnlyCollection<(string Key, string Value)>? Attributes, Guid? VariantId)> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _items.Clear();
        InvalidateItemsSnapshot();

        foreach (var item in items)
        {
            AddItem(item.Name, item.Description, item.Type, item.ReferenceId, item.Quantity, item.UnitPrice, item.DiscountAmount, item.Attributes, item.VariantId);
        }

        EvaluateStatus(null);
        Touch();
    }

    public bool RemoveItem(Guid itemId)
    {
        var removed = _items.RemoveAll(item => item.Id == itemId) > 0;

        if (removed)
        {
            InvalidateItemsSnapshot();
            EvaluateStatus(null);
            Touch();
        }

        return removed;
    }

    public PaymentTransaction AddTransaction(
        decimal amount,
        PaymentMethod method,
        TransactionStatus status,
        string reference,
        string? gateway,
        string? description,
        string? metadata)
    {
        var transaction = new PaymentTransaction(this, amount, method, status, reference, gateway, description, metadata);
        _transactions.Add(transaction);
        InvalidateTransactionsSnapshot();
        EvaluateStatus(null);
        Touch();
        return transaction;
    }

    public PaymentTransaction UpdateTransaction(
        Guid transactionId,
        TransactionStatus status,
        string? description,
        string? metadata,
        DateTimeOffset? occurredAt,
        decimal? amount = null)
    {
        var transaction = _transactions.FirstOrDefault(t => t.Id == transactionId)
            ?? throw new DomainException("تراکنش مورد نظر یافت نشد.");

        transaction.SetStatus(status);

        if (amount is not null)
        {
            transaction.SetAmount(amount.Value);
        }

        transaction.SetDescription(description);
        transaction.SetMetadata(metadata);

        if (occurredAt.HasValue)
        {
            transaction.OccurredOn(occurredAt.Value);
        }

        InvalidateTransactionsSnapshot();
        EvaluateStatus(occurredAt);
        Touch();

        return transaction;
    }

    private void InvalidateItemsSnapshot()
        => _itemsSnapshot = null;

    private void InvalidateTransactionsSnapshot()
        => _transactionsSnapshot = null;

    public void EvaluateStatus(DateTimeOffset? referenceTime)
    {
        if (Status == InvoiceStatus.Cancelled)
        {
            return;
        }

        var now = referenceTime ?? DateTimeOffset.Now;

        var outstanding = OutstandingAmount;
        var paid = PaidAmount;

        if (outstanding <= 0)
        {
            Status = InvoiceStatus.Paid;
        }
        else if (paid > 0)
        {
            Status = InvoiceStatus.PartiallyPaid;
        }
        else
        {
            Status = InvoiceStatus.Pending;
        }

        if (Status is InvoiceStatus.Pending or InvoiceStatus.PartiallyPaid && DueDate is not null && DueDate.Value < now)
        {
            Status = InvoiceStatus.Overdue;
        }
    }

    public void Cancel()
    {
        Status = InvoiceStatus.Cancelled;
        Touch();
    }

    public void Reopen()
    {
        if (Status != InvoiceStatus.Cancelled)
        {
            return;
        }

        Status = InvoiceStatus.Pending;
        EvaluateStatus(null);
        Touch();
    }

    internal void Touch()
    {
        UpdateDate = DateTimeOffset.Now;
    }

    private static decimal RoundMoney(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
