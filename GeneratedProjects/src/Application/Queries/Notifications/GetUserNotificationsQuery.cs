using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Notifications;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Notifications;

public sealed record GetUserNotificationsQuery(string UserId, string? UserRole = null, bool? IsRead = null) : IQuery<NotificationListDto>
{
    public sealed class Handler : IQueryHandler<GetUserNotificationsQuery, NotificationListDto>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly Microsoft.AspNetCore.Identity.UserManager<TestAttarClone.Domain.Entities.ApplicationUser> _userManager;

        public Handler(INotificationRepository notificationRepository, Microsoft.AspNetCore.Identity.UserManager<TestAttarClone.Domain.Entities.ApplicationUser> userManager)
        {
            _notificationRepository = notificationRepository;
            _userManager = userManager;
        }

        public async Task<Result<NotificationListDto>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
        {
            var userNotifications = await _notificationRepository.GetUserNotificationsAsync(
                request.UserId,
                request.UserRole,
                request.IsRead,
                cancellationToken);

                var unreadCount = await _notificationRepository.GetUnreadCountAsync(request.UserId, request.UserRole, cancellationToken);

            var dtos = new List<NotificationDto>();

            foreach (var un in userNotifications)
            {
                var createdById = un.Notification.CreatedById;
                var createdByDisplay = un.Notification.CreatedBy?.FullName ?? un.Notification.CreatedBy?.UserName ?? "سیستم";
                var createdByIsAdmin = false;

                if (!string.IsNullOrWhiteSpace(createdById))
                {
                    try
                    {
                        var createdByUser = await _userManager.FindByIdAsync(createdById!);
                        if (createdByUser is not null)
                        {
                            createdByIsAdmin = await _userManager.IsInRoleAsync(createdByUser, "Admin");
                        }
                    }
                    catch
                    {
                        // ignore role-check errors and treat as non-admin
                        createdByIsAdmin = false;
                    }
                }

                dtos.Add(new NotificationDto(
                    un.Notification.Id,
                    un.Notification.Title,
                    un.Notification.Message,
                    un.Notification.Type,
                    un.Notification.Priority,
                    un.Notification.SentAt,
                    un.Notification.ExpiresAt,
                    createdByDisplay,
                    un.IsRead,
                    un.ReadAt,
                    createdById,
                    createdByIsAdmin));
            }

            var result = new NotificationListDto(
                dtos,
                dtos.Count,
                unreadCount);

            return Result<NotificationListDto>.Success(result);
        }
    }
}

