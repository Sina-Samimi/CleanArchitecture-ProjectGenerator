using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Tickets;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities;
using LogsDtoCloneTest.Domain.Entities.Notifications;
using LogsDtoCloneTest.Domain.Entities.Tickets;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.Authorization;
using LogsDtoCloneTest.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace LogsDtoCloneTest.Application.Commands.Tickets;

public sealed record CreateTicketCommand(
    string UserId,
    string Subject,
    string Message,
    string? Department = null,
    string? AttachmentPath = null) : ICommand<TicketDto>
{
    public sealed class Handler : ICommandHandler<CreateTicketCommand, TicketDto>
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditContext _auditContext;

        public Handler(
            ITicketRepository ticketRepository,
            INotificationRepository notificationRepository,
            UserManager<ApplicationUser> userManager,
            IAuditContext auditContext)
        {
            _ticketRepository = ticketRepository;
            _notificationRepository = notificationRepository;
            _userManager = userManager;
            _auditContext = auditContext;
        }

        public async Task<Result<TicketDto>> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return Result<TicketDto>.Failure("شناسه کاربر الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                return Result<TicketDto>.Failure("موضوع تیکت الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Result<TicketDto>.Failure("متن تیکت الزامی است.");
            }

            var ticket = new Ticket(
                request.UserId,
                request.Subject,
                request.Message,
                request.Department,
                request.AttachmentPath);

            var audit = _auditContext.Capture();
            ticket.CreatorId = audit.UserId;
            ticket.CreateDate = audit.Timestamp;
            ticket.UpdateDate = audit.Timestamp;
            ticket.Ip = audit.IpAddress;

            await _ticketRepository.AddAsync(ticket, cancellationToken);

            var ticketWithUser = await _ticketRepository.GetByIdAsync(ticket.Id, cancellationToken);
            if (ticketWithUser is null)
            {
                return Result<TicketDto>.Failure("خطا در ایجاد تیکت.");
            }

            var dto = new TicketDto(
                ticketWithUser.Id,
                ticketWithUser.UserId,
                ticketWithUser.User?.UserName ?? "نامشخص",
                ticketWithUser.User?.FullName ?? "نامشخص",
                ticketWithUser.User?.PhoneNumber,
                ticketWithUser.Subject,
                ticketWithUser.Message,
                ticketWithUser.Department,
                ticketWithUser.AttachmentPath,
                ticketWithUser.Status,
                ticketWithUser.AssignedToId,
                ticketWithUser.AssignedTo?.FullName,
                ticketWithUser.CreateDate,
                ticketWithUser.LastReplyDate,
                ticketWithUser.HasUnreadReplies,
                0);

            // Create notification ONLY for admins about new ticket
            // DO NOT send to regular users
            try
            {
                // Get all admin users (only admins should receive ticket notifications)
                var adminUsers = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
                if (adminUsers.Count > 0)
                {
                    var notification = new Notification(
                        "تیکت جدید",
                        $"تیکت جدید با موضوع «{ticketWithUser.Subject}» از کاربر {ticketWithUser.User?.FullName ?? ticketWithUser.User?.UserName ?? "نامشخص"} ثبت شد.",
                        NotificationType.System,
                        NotificationPriority.Normal,
                        null);

                    notification.CreatorId = audit.UserId;
                    notification.CreateDate = audit.Timestamp;
                    notification.UpdateDate = audit.Timestamp;
                    notification.Ip = audit.IpAddress;
                    notification.SetCreatedBy(audit.UserId);

                    await _notificationRepository.AddAsync(notification, cancellationToken);

                    // Only send to active admin users (exclude the user who created the ticket if they are an admin)
                    // IMPORTANT: Filter to ensure only admins get this notification, never regular users
                    var userNotifications = adminUsers
                        .Where(u => u.IsActive 
                            && !u.IsDeleted 
                            && u.Id != request.UserId 
                            && u.Id != audit.UserId)
                        .Select(admin => new UserNotification(notification.Id, admin.Id))
                        .ToList();

                    foreach (var userNotification in userNotifications)
                    {
                        userNotification.CreatorId = audit.UserId;
                        userNotification.CreateDate = audit.Timestamp;
                        userNotification.UpdateDate = audit.Timestamp;
                        userNotification.Ip = audit.IpAddress;
                    }

                    if (userNotifications.Count > 0)
                    {
                        await _notificationRepository.AddUserNotificationsAsync(userNotifications, cancellationToken);
                    }
                }
            }
            catch
            {
                // Silently fail notification creation - don't break ticket creation
            }

            return Result<TicketDto>.Success(dto);
        }
    }
}
