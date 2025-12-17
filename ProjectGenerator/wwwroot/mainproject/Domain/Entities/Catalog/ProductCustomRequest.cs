using System;
using System.Diagnostics.CodeAnalysis;
using Attar.Domain.Base;
using Attar.Domain.Enums;

namespace Attar.Domain.Entities.Catalog;

public sealed class ProductCustomRequest : Entity
{
    public Guid ProductId { get; private set; }

    public Product? Product { get; private set; }

    public string? UserId { get; private set; }

    public string FullName { get; private set; }

    public string Phone { get; private set; }

    public string? Email { get; private set; }

    public string? Message { get; private set; }

    public CustomRequestStatus Status { get; private set; }

    public DateTimeOffset? ContactedAt { get; private set; }

    public string? AdminNotes { get; private set; }

    [SetsRequiredMembers]
    private ProductCustomRequest()
    {
        FullName = string.Empty;
        Phone = string.Empty;
    }

    [SetsRequiredMembers]
    public ProductCustomRequest(
        Guid productId,
        string fullName,
        string phone,
        string? email = null,
        string? message = null,
        string? userId = null)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Phone cannot be empty", nameof(phone));
        }

        ProductId = productId;
        FullName = fullName.Trim();
        Phone = phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
        Status = CustomRequestStatus.Pending;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void MarkAsContacted()
    {
        if (Status == CustomRequestStatus.Pending)
        {
            Status = CustomRequestStatus.Contacted;
            ContactedAt = DateTimeOffset.UtcNow;
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }

    public void MarkAsCompleted()
    {
        if (Status is CustomRequestStatus.Pending or CustomRequestStatus.Contacted)
        {
            Status = CustomRequestStatus.Completed;
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }

    public void MarkAsCancelled()
    {
        if (Status != CustomRequestStatus.Completed)
        {
            Status = CustomRequestStatus.Cancelled;
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }

    public void UpdateStatus(CustomRequestStatus status)
    {
        Status = status;
        if (status == CustomRequestStatus.Contacted && ContactedAt is null)
        {
            ContactedAt = DateTimeOffset.UtcNow;
        }
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateAdminNotes(string? notes)
    {
        AdminNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }
}

