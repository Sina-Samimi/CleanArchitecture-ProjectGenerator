using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Notifications;
using Attar.Application.Interfaces;
using Attar.Domain.Entities;
using Attar.Domain.Entities.Notifications;
using Attar.Domain.Enums;
using Attar.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Attar.Application.Commands.Notifications;

public sealed record CreateNotificationCommand(CreateNotificationDto Dto) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<CreateNotificationCommand, Guid>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditContext _auditContext;

        public Handler(
            INotificationRepository notificationRepository,
            UserManager<ApplicationUser> userManager,
            IAuditContext auditContext)
        {
            _notificationRepository = notificationRepository;
            _userManager = userManager;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return Result<Guid>.Failure("عنوان اعلان نمی‌تواند خالی باشد.");
            }

            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                return Result<Guid>.Failure("متن اعلان نمی‌تواند خالی باشد.");
            }

            // Create notification
            var notification = new Notification(
                dto.Title,
                dto.Message,
                dto.Type,
                dto.Priority,
                dto.ExpiresAt);

            var audit = _auditContext.Capture();
            notification.CreatorId = audit.UserId;
            notification.CreateDate = audit.Timestamp;
            notification.UpdateDate = audit.Timestamp;
            notification.Ip = audit.IpAddress;
            notification.SetCreatedBy(audit.UserId);

            await _notificationRepository.AddAsync(notification, cancellationToken);

            // Get target users based on filter
            var targetUserIds = await GetTargetUserIdsAsync(dto.Filter, cancellationToken);

            if (targetUserIds.Count == 0)
            {
                // Deactivate notification if no users found
                notification.Deactivate();
                await _notificationRepository.UpdateAsync(notification, cancellationToken);
                return Result<Guid>.Failure("هیچ کاربری با فیلترهای مشخص شده یافت نشد.");
            }

            // Create UserNotification for each target user
            var userNotifications = new List<UserNotification>();
            var targetRoles = dto.Filter.RoleNames?.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();

            foreach (var userId in targetUserIds)
            {
                // Determine which role(s) this user is being targeted under
                string? targetRole = null;

                // If specific roles were selected, find which one applies to this user
                if (targetRoles is not null && targetRoles.Count > 0)
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user is not null)
                    {
                        // Find the first matching role for this user from the target roles
                        foreach (var role in targetRoles)
                        {
                            if (await _userManager.IsInRoleAsync(user, role))
                            {
                                targetRole = role;
                                break;
                            }
                        }
                    }
                }

                var userNotification = new UserNotification(notification.Id, userId, targetRole);
                userNotifications.Add(userNotification);
            }

            // Set audit fields for user notifications
            foreach (var userNotification in userNotifications)
            {
                userNotification.CreatorId = audit.UserId;
                userNotification.CreateDate = audit.Timestamp;
                userNotification.UpdateDate = audit.Timestamp;
                userNotification.Ip = audit.IpAddress;
            }

            await _notificationRepository.AddUserNotificationsAsync(userNotifications, cancellationToken);

            return Result<Guid>.Success(notification.Id);
        }

        private async Task<IReadOnlyCollection<string>> GetTargetUserIdsAsync(
            NotificationFilterDto filter,
            CancellationToken cancellationToken)
        {
            var query = _userManager.Users
                .Where(user => !user.IsDeleted && user.IsActive)
                .AsQueryable();

            // Check if any role filter is explicitly specified
            var hasRoleFilter = filter.RoleNames is not null && filter.RoleNames.Count > 0;
            var roleNames = hasRoleFilter 
                ? filter.RoleNames.Where(r => !string.IsNullOrWhiteSpace(r)).ToList() 
                : new List<string>();

            // Filter by roles
            if (hasRoleFilter && roleNames.Count > 0)
            {
                var usersWithRoles = new List<string>();
                foreach (var roleName in roleNames)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                    usersWithRoles.AddRange(usersInRole.Select(u => u.Id));
                }
                query = query.Where(user => usersWithRoles.Contains(user.Id));
            }
            else
            {
                // If no role filter specified, exclude sellers from notifications
                // (Notifications sent by admin are only for Admins and regular Users, not Sellers)
                var sellers = await _userManager.GetUsersInRoleAsync("Seller");
                var sellerIds = new HashSet<string>(sellers.Select(s => s.Id), StringComparer.OrdinalIgnoreCase);
                query = query.Where(user => !sellerIds.Contains(user.Id));
            }

            // Filter by registration date
            if (filter.RegisteredFrom.HasValue)
            {
                query = query.Where(user => user.CreatedOn >= filter.RegisteredFrom.Value);
            }

            if (filter.RegisteredTo.HasValue)
            {
                query = query.Where(user => user.CreatedOn < filter.RegisteredTo.Value);
            }

            // Filter by selected user IDs
            if (filter.SelectedUserIds is not null && filter.SelectedUserIds.Count > 0)
            {
                var selectedIds = filter.SelectedUserIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
                if (selectedIds.Count > 0)
                {
                    query = query.Where(user => selectedIds.Contains(user.Id));
                }
            }

            var userIds = await query
                .Select(user => user.Id)
                .Distinct()
                .ToListAsync(cancellationToken);

            return userIds;
        }
    }
}

