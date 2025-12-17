using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Domain.Entities.Notifications;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task AddUserNotificationAsync(UserNotification userNotification, CancellationToken cancellationToken);
    Task AddUserNotificationsAsync(IReadOnlyCollection<UserNotification> userNotifications, CancellationToken cancellationToken);
    Task<UserNotification?> GetUserNotificationAsync(Guid notificationId, string userId, CancellationToken cancellationToken);
    Task UpdateUserNotificationAsync(UserNotification userNotification, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserNotification>> GetUserNotificationsAsync(string userId, bool? isRead = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserNotification>> GetUserNotificationsAsync(string userId, string? userRole, bool? isRead = null, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken);
    Task<int> GetUnreadCountAsync(string userId, string? userRole, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Notification>> GetAdminCreatedNotificationsAsync(
        string adminId,
        string? searchTitle = null,
        string? searchMessage = null,
        int? typeFilter = null,
        int? priorityFilter = null,
        DateTimeOffset? dateFromFilter = null,
        DateTimeOffset? dateToFilter = null,
        bool? isActiveFilter = null,
        CancellationToken cancellationToken = default);
}


