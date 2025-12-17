using System;
using System.Diagnostics.CodeAnalysis;
using LogsDtoCloneTest.Domain.Base;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.Domain.Exceptions;

namespace LogsDtoCloneTest.Domain.Entities.Billing;

public sealed class PaymentTransaction : Entity
{
    public Guid InvoiceId { get; private set; }

    public Invoice Invoice { get; private set; } = null!;

    public decimal Amount { get; private set; }

    public PaymentMethod Method { get; private set; }

    public TransactionStatus Status { get; private set; }

    public string Reference { get; private set; } = null!;

    public string? GatewayName { get; private set; }

    public string? Description { get; private set; }

    public string? Metadata { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    [SetsRequiredMembers]
    private PaymentTransaction()
    {
        Reference = string.Empty;
    }

    [SetsRequiredMembers]
    internal PaymentTransaction(
        Invoice invoice,
        decimal amount,
        PaymentMethod method,
        TransactionStatus status,
        string reference,
        string? gatewayName,
        string? description,
        string? metadata)
    {
        AssignToInvoice(invoice);
        SetAmount(amount);
        SetReference(reference);
        SetMethod(method);
        SetStatus(status);
        SetGateway(gatewayName);
        SetDescription(description);
        SetMetadata(metadata);
        OccurredAt = DateTimeOffset.Now;
    }

    internal void AssignToInvoice(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        Invoice = invoice;
        InvoiceId = invoice.Id;
    }

    public void SetAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Transaction amount must be greater than zero.");
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    public void SetMethod(PaymentMethod method)
    {
        if (!Enum.IsDefined(typeof(PaymentMethod), method))
        {
            method = PaymentMethod.Unknown;
        }

        Method = method;
    }

    public void SetStatus(TransactionStatus status)
    {
        if (!Enum.IsDefined(typeof(TransactionStatus), status))
        {
            throw new DomainException("Transaction status is invalid.");
        }

        Status = status;
    }

    public void SetReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new DomainException("Transaction reference is required.");
        }

        Reference = reference.Trim();
    }

    public void SetGateway(string? gatewayName)
    {
        GatewayName = string.IsNullOrWhiteSpace(gatewayName) ? null : gatewayName.Trim();
    }

    public void SetDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetMetadata(string? metadata)
    {
        Metadata = string.IsNullOrWhiteSpace(metadata) ? null : metadata.Trim();
    }

    public void OccurredOn(DateTimeOffset timestamp)
    {
        OccurredAt = timestamp;
    }
}
