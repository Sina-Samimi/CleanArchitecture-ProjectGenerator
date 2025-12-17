using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Attar.Domain.Base;
using Attar.Domain.Enums;
using Attar.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Attar.Domain.Entities.Billing;

public sealed class InvoiceItem : Entity
{
    private readonly List<InvoiceItemAttribute> _attributes = new();
    private IReadOnlyCollection<InvoiceItemAttribute>? _attributesSnapshot;

    public Guid InvoiceId { get; private set; }

    public Invoice Invoice { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public InvoiceItemType ItemType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public Guid? VariantId { get; private set; } // Variant انتخاب شده برای محصول

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal? DiscountAmount { get; private set; }

    [BackingField(nameof(_attributes))]
    public IReadOnlyCollection<InvoiceItemAttribute> Attributes => _attributesSnapshot ??= _attributes.AsReadOnly();

    public decimal Subtotal => RoundMoney(Quantity * UnitPrice);

    public decimal Total => RoundMoney(Subtotal - (DiscountAmount ?? 0m));

    [SetsRequiredMembers]
    private InvoiceItem()
    {
        Name = string.Empty;
    }

    [SetsRequiredMembers]
    internal InvoiceItem(
        Invoice invoice,
        string name,
        string? description,
        InvoiceItemType itemType,
        Guid? referenceId,
        decimal quantity,
        decimal unitPrice,
        decimal? discountAmount,
        Guid? variantId = null)
    {
        AssignToInvoice(invoice);
        UpdateSnapshot(name, description, itemType, referenceId, quantity, unitPrice, discountAmount, variantId);
    }

    internal void AssignToInvoice(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        Invoice = invoice;
        InvoiceId = invoice.Id;
    }

    public void UpdateSnapshot(
        string name,
        string? description,
        InvoiceItemType itemType,
        Guid? referenceId,
        decimal quantity,
        decimal unitPrice,
        decimal? discountAmount,
        Guid? variantId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Invoice item name is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Invoice item quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Invoice item unit price cannot be negative.");
        }

        if (discountAmount is not null && discountAmount.Value < 0)
        {
            throw new DomainException("Invoice item discount cannot be negative.");
        }

        var subtotal = quantity * unitPrice;
        if (discountAmount is not null && discountAmount.Value > subtotal)
        {
            throw new DomainException("Invoice item discount cannot exceed subtotal.");
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ItemType = itemType;
        ReferenceId = referenceId.HasValue && referenceId.Value == Guid.Empty ? null : referenceId;
        VariantId = variantId.HasValue && variantId.Value == Guid.Empty ? null : variantId;
        Quantity = RoundMoney(quantity);
        UnitPrice = RoundMoney(unitPrice);
        DiscountAmount = discountAmount is null ? null : RoundMoney(discountAmount.Value);
    }

    public InvoiceItemAttribute AddAttribute(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("Attribute key is required.");
        }

        var normalizedKey = key.Trim();

        var existing = _attributes.FirstOrDefault(attribute => attribute.Key.Equals(normalizedKey, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.SetValue(value);
            return existing;
        }

        var attribute = new InvoiceItemAttribute(this, normalizedKey, value);
        _attributes.Add(attribute);
        return attribute;
    }

    public bool RemoveAttribute(Guid attributeId)
        => _attributes.RemoveAll(attribute => attribute.Id == attributeId) > 0;

    public void ClearAttributes()
        => _attributes.Clear();

    private static decimal RoundMoney(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
