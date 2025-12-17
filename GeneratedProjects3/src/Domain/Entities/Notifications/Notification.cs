using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LogTableRenameTest.Domain.Base;
using LogTableRenameTest.Domain.Entities;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.Domain.Entities.Notifications;

public sealed class Notification : Entity
{
    private readonly List<UserNotification> _userNotifications = new();

    [SetsRequiredMembers]
    private Notification()
    {
    }

    [SetsRequiredMembers]
    public Notification(
        string title,
        string message,
        NotificationType type,
        NotificationPriority priority,
        DateTimeOffset? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(message));
        }

        Title = title.Trim();
        Message = message.Trim();
        Type = type;
        Priority = priority;
        ExpiresAt = expiresAt;
        IsActive = true;
        SentAt = DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public string Title { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public NotificationType Type { get; private set; }

    public NotificationPriority Priority { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset SentAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public string? CreatedById { get; private set; }

    public ApplicationUser? CreatedBy { get; private set; }

    public IReadOnlyCollection<UserNotification> UserNotifications => _userNotifications.AsReadOnly();

    public void SetCreatedBy(string? userId)
    {
        CreatedById = userId;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateContent(string title, string message)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(message));
        }

        Title = title.Trim();
        Message = message.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetExpiresAt(DateTimeOffset? expiresAt)
    {
        ExpiresAt = expiresAt;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}

