using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Entities.Notifications;
using MobiRooz.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MobiRooz.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _dbContext;

    public NotificationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.Notifications
            .Include(n => n.UserNotifications)
            .Include(n => n.CreatedBy)
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken);

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        _dbContext.Notifications.Update(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var notification = await GetByIdAsync(id, cancellationToken);
        if (notification != null)
        {
            notification.Deactivate();
            _dbContext.Notifications.Update(notification);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddUserNotificationAsync(UserNotification userNotification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userNotification);
        await _dbContext.UserNotifications.AddAsync(userNotification, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddUserNotificationsAsync(IReadOnlyCollection<UserNotification> userNotifications, CancellationToken cancellationToken)
    {
        if (userNotifications is null || userNotifications.Count == 0)
        {
            return;
        }

        await _dbContext.UserNotifications.AddRangeAsync(userNotifications, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserNotification?> GetUserNotificationAsync(Guid notificationId, string userId, CancellationToken cancellationToken)
        => await _dbContext.UserNotifications
            .Include(un => un.Notification)
            .FirstOrDefaultAsync(un => un.NotificationId == notificationId && un.UserId == userId && !un.IsDeleted, cancellationToken);

    public async Task UpdateUserNotificationAsync(UserNotification userNotification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userNotification);
        _dbContext.UserNotifications.Update(userNotification);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserNotification>> GetUserNotificationsAsync(string userId, bool? isRead = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserNotifications
            .Include(un => un.Notification)
                .ThenInclude(n => n.CreatedBy)
            .Where(un => un.UserId == userId && !un.IsDeleted && !un.Notification.IsDeleted && un.Notification.IsActive);

        if (isRead.HasValue)
        {
            query = query.Where(un => un.IsRead == isRead.Value);
        }

        // Filter expired notifications
        var now = DateTimeOffset.UtcNow;
        query = query.Where(un => un.Notification.ExpiresAt == null || un.Notification.ExpiresAt > now);

        return await query
            .OrderByDescending(un => un.Notification.SentAt)
            .ThenByDescending(un => un.Notification.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserNotification>> GetUserNotificationsAsync(string userId, string? userRole, bool? isRead = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserNotifications
            .Include(un => un.Notification)
                .ThenInclude(n => n.CreatedBy)
            .Where(un => un.UserId == userId && !un.IsDeleted && !un.Notification.IsDeleted && un.Notification.IsActive);

        if (isRead.HasValue)
        {
            query = query.Where(un => un.IsRead == isRead.Value);
        }

        // Filter by target role if specified
        // Only include notifications that either have no target role (broadcast) or match the user's current role
        if (!string.IsNullOrWhiteSpace(userRole))
        {
            query = query.Where(un => un.TargetRole == null || un.TargetRole == userRole);
        }
        else
        {
            // If no role specified, only show broadcast notifications (TargetRole == null)
            query = query.Where(un => un.TargetRole == null);
        }

        // Filter expired notifications
        var now = DateTimeOffset.UtcNow;
        query = query.Where(un => un.Notification.ExpiresAt == null || un.Notification.ExpiresAt > now);

        return await query
            .OrderByDescending(un => un.Notification.SentAt)
            .ThenByDescending(un => un.Notification.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await _dbContext.UserNotifications
            .Where(un => un.UserId == userId 
                && !un.IsDeleted 
                && !un.IsRead
                && !un.Notification.IsDeleted 
                && un.Notification.IsActive
                && (un.Notification.ExpiresAt == null || un.Notification.ExpiresAt > now))
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(string userId, string? userRole, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var query = _dbContext.UserNotifications
            .Where(un => un.UserId == userId
                && !un.IsDeleted
                && !un.IsRead
                && !un.Notification.IsDeleted
                && un.Notification.IsActive);

        // Filter by target role if specified
        if (!string.IsNullOrWhiteSpace(userRole))
        {
            query = query.Where(un => un.TargetRole == null || un.TargetRole == userRole);
        }
        else
        {
            // If no role specified, only count broadcast notifications
            query = query.Where(un => un.TargetRole == null);
        }

        query = query.Where(un => un.Notification.ExpiresAt == null || un.Notification.ExpiresAt > now);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Notification>> GetAdminCreatedNotificationsAsync(
        string adminId,
        string? searchTitle = null,
        string? searchMessage = null,
        int? typeFilter = null,
        int? priorityFilter = null,
        DateTimeOffset? dateFromFilter = null,
        DateTimeOffset? dateToFilter = null,
        bool? isActiveFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Notifications
            .Include(n => n.UserNotifications)
            .Include(n => n.CreatedBy)
            .Where(n => n.CreatedById == adminId && !n.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTitle))
        {
            query = query.Where(n => n.Title.Contains(searchTitle));
        }

        if (!string.IsNullOrWhiteSpace(searchMessage))
        {
            query = query.Where(n => n.Message.Contains(searchMessage));
        }

        if (typeFilter.HasValue)
        {
            query = query.Where(n => (int)n.Type == typeFilter.Value);
        }

        if (priorityFilter.HasValue)
        {
            query = query.Where(n => (int)n.Priority == priorityFilter.Value);
        }

        if (dateFromFilter.HasValue)
        {
            query = query.Where(n => n.SentAt >= dateFromFilter.Value);
        }

        if (dateToFilter.HasValue)
        {
            query = query.Where(n => n.SentAt <= dateToFilter.Value);
        }

        if (isActiveFilter.HasValue)
        {
            query = query.Where(n => n.IsActive == isActiveFilter.Value);
        }

        return await query
            .OrderByDescending(n => n.SentAt)
            .ToListAsync(cancellationToken);
    }
}
