using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TestAttarClone.Domain.Base;
using TestAttarClone.Domain.Entities;
using TestAttarClone.Domain.Enums;

namespace TestAttarClone.Domain.Entities.Tickets;

public sealed class Ticket : Entity
{
    private readonly List<TicketReply> _replies = new();

    [SetsRequiredMembers]
    private Ticket()
    {
        Subject = string.Empty;
        Message = string.Empty;
    }

    [SetsRequiredMembers]
    public Ticket(
        string userId,
        string subject,
        string message,
        string? department = null,
        string? attachmentPath = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Subject cannot be empty", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(message));
        }

        UserId = userId.Trim();
        Subject = subject.Trim();
        Message = message.Trim();
        Department = string.IsNullOrWhiteSpace(department) ? null : department.Trim();
        AttachmentPath = string.IsNullOrWhiteSpace(attachmentPath) ? null : attachmentPath.Trim();
        Status = TicketStatus.Pending;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public string UserId { get; private set; } = string.Empty;

    public ApplicationUser User { get; private set; } = null!;

    public string Subject { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public string? Department { get; private set; }

    public string? AttachmentPath { get; private set; }

    public TicketStatus Status { get; private set; }

    public string? AssignedToId { get; private set; }

    public ApplicationUser? AssignedTo { get; private set; }

    public DateTimeOffset? LastReplyDate { get; private set; }

    public bool HasUnreadReplies { get; private set; }

    public IReadOnlyCollection<TicketReply> Replies => _replies.AsReadOnly();

    public void UpdateStatus(TicketStatus status)
    {
        Status = status;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void AssignTo(string? adminId)
    {
        AssignedToId = string.IsNullOrWhiteSpace(adminId) ? null : adminId.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void MarkAsRead()
    {
        HasUnreadReplies = false;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void MarkAsUnread()
    {
        HasUnreadReplies = true;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateLastReplyDate(DateTimeOffset date)
    {
        LastReplyDate = date;
        HasUnreadReplies = true;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
