using System;
using System.Collections.Generic;
using Attar.Domain.Enums;

namespace Attar.Application.DTOs.Notifications;

public sealed record NotificationFilterDto(
    IReadOnlyCollection<string>? RoleNames = null,
    DateTimeOffset? RegisteredFrom = null,
    DateTimeOffset? RegisteredTo = null,
    IReadOnlyCollection<string>? SelectedUserIds = null);

public sealed record CreateNotificationDto(
    string Title,
    string Message,
    NotificationType Type,
    NotificationPriority Priority,
    DateTimeOffset? ExpiresAt,
    NotificationFilterDto Filter);

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    NotificationPriority Priority,
    DateTimeOffset SentAt,
    DateTimeOffset? ExpiresAt,
    string? CreatedByDisplayName,
    bool IsRead,
    DateTimeOffset? ReadAt,
    string? CreatedById,
    bool CreatedByIsAdmin);

public sealed record NotificationListDto(
    IReadOnlyCollection<NotificationDto> Items,
    int TotalCount,
    int UnreadCount);

public sealed record AdminNotificationDto(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    NotificationPriority Priority,
    DateTimeOffset SentAt,
    DateTimeOffset? ExpiresAt,
    bool IsActive,
    int RecipientCount);

public sealed record AdminNotificationStatsDto(
    int Total,
    int Active,
    int Inactive,
    int Expired);

public sealed record AdminNotificationsListDto(
    IReadOnlyCollection<AdminNotificationDto> Items,
    AdminNotificationStatsDto Stats);


