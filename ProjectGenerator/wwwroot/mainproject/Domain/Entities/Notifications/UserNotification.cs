using System;
using System.Diagnostics.CodeAnalysis;
using Attar.Domain.Base;
using Attar.Domain.Entities;

namespace Attar.Domain.Entities.Notifications;

public sealed class UserNotification : Entity
{
    [SetsRequiredMembers]
    private UserNotification()
    {
    }

    [SetsRequiredMembers]
    public UserNotification(Guid notificationId, string userId, string? targetRole = null)
    {
        if (notificationId == Guid.Empty)
        {
            throw new ArgumentException("Notification ID cannot be empty", nameof(notificationId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        NotificationId = notificationId;
        UserId = userId;
        TargetRole = string.IsNullOrWhiteSpace(targetRole) ? null : targetRole.Trim();
        IsRead = false;
        ReadAt = null;
        CreateDate = DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public Guid NotificationId { get; private set; }

    public Notification Notification { get; private set; } = null!;

    public string UserId { get; private set; } = string.Empty;

    public ApplicationUser User { get; private set; } = null!;

    public string? TargetRole { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public void MarkAsRead()
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAt = DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void MarkAsUnread()
    {
        if (!IsRead)
        {
            return;
        }

        IsRead = false;
        ReadAt = null;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}

