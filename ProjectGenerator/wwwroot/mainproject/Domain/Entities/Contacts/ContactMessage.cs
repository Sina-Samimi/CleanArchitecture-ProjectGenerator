using System;
using System.Diagnostics.CodeAnalysis;
using MobiRooz.Domain.Base;

namespace MobiRooz.Domain.Entities.Contacts;

public sealed class ContactMessage : Entity
{
    public string? UserId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public bool IsRead { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public string? ReadByUserId { get; private set; }

    public string? AdminReply { get; private set; }

    public DateTimeOffset? RepliedAt { get; private set; }

    public string? RepliedByUserId { get; private set; }

    [SetsRequiredMembers]
    private ContactMessage()
    {
    }

    [SetsRequiredMembers]
    public ContactMessage(
        string fullName,
        string email,
        string phone,
        string subject,
        string message,
        string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Phone cannot be empty", nameof(phone));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Subject cannot be empty", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(message));
        }

        UserId = userId;
        FullName = fullName.Trim();
        Email = email.Trim();
        Phone = phone.Trim();
        Subject = subject.Trim();
        Message = message.Trim();
        IsRead = false;
    }

    public void MarkAsRead(string readByUserId)
    {
        if (string.IsNullOrWhiteSpace(readByUserId))
        {
            throw new ArgumentException("Read by user ID cannot be empty", nameof(readByUserId));
        }

        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAt = DateTimeOffset.UtcNow;
        ReadByUserId = readByUserId;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void AddAdminReply(string reply, string repliedByUserId)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new ArgumentException("Reply cannot be empty", nameof(reply));
        }

        if (string.IsNullOrWhiteSpace(repliedByUserId))
        {
            throw new ArgumentException("Replied by user ID cannot be empty", nameof(repliedByUserId));
        }

        AdminReply = reply.Trim();
        RepliedAt = DateTimeOffset.UtcNow;
        RepliedByUserId = repliedByUserId;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}

