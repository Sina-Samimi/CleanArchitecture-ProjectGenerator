using System;
using System.Diagnostics.CodeAnalysis;
using Attar.Domain.Base;
using Attar.Domain.Enums;

namespace Attar.Domain.Entities.Orders;

public sealed class ShipmentTracking : Entity
{
    public Guid InvoiceItemId { get; private set; }

    public Billing.InvoiceItem InvoiceItem { get; private set; } = null!;

    public ShipmentStatus Status { get; private set; }

    public string? TrackingNumber { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset StatusDate { get; private set; }

    public string? UpdatedById { get; private set; }

    public ApplicationUser? UpdatedBy { get; private set; }

    [SetsRequiredMembers]
    private ShipmentTracking()
    {
    }

    [SetsRequiredMembers]
    public ShipmentTracking(
        Guid invoiceItemId,
        ShipmentStatus status,
        DateTimeOffset statusDate,
        string? trackingNumber = null,
        string? notes = null)
    {
        if (invoiceItemId == Guid.Empty)
        {
            throw new ArgumentException("Invoice item ID cannot be empty", nameof(invoiceItemId));
        }

        InvoiceItemId = invoiceItemId;
        Status = status;
        StatusDate = statusDate;
        TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(
        ShipmentStatus status,
        DateTimeOffset statusDate,
        string? trackingNumber = null,
        string? notes = null)
    {
        Status = status;
        StatusDate = statusDate;
        TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetTrackingNumber(string? trackingNumber)
    {
        TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetNotes(string? notes)
    {
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetUpdatedBy(string? userId)
    {
        UpdatedById = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }
}

