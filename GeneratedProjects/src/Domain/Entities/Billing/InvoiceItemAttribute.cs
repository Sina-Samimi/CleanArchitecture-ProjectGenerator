using System;
using System.Diagnostics.CodeAnalysis;
using TestAttarClone.Domain.Base;
using TestAttarClone.Domain.Exceptions;

namespace TestAttarClone.Domain.Entities.Billing;

public sealed class InvoiceItemAttribute : Entity
{
    public Guid InvoiceItemId { get; private set; }

    public InvoiceItem InvoiceItem { get; private set; } = null!;

    public string Key { get; private set; } = null!;

    public string Value { get; private set; } = null!;

    [SetsRequiredMembers]
    private InvoiceItemAttribute()
    {
        Key = string.Empty;
        Value = string.Empty;
    }

    [SetsRequiredMembers]
    internal InvoiceItemAttribute(InvoiceItem item, string key, string value)
    {
        AssignToItem(item);
        SetKey(key);
        SetValue(value);
    }

    internal void AssignToItem(InvoiceItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        InvoiceItem = item;
        InvoiceItemId = item.Id;
    }

    public void SetKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("Attribute key cannot be empty.");
        }

        Key = key.Trim();
    }

    public void SetValue(string value)
    {
        Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
