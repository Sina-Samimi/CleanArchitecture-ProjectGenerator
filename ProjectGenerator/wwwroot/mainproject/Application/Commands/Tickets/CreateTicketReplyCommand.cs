using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs.Tickets;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Entities;
using MobiRooz.Domain.Entities.Notifications;
using MobiRooz.Domain.Entities.Tickets;
using MobiRooz.Domain.Enums;
using MobiRooz.SharedKernel.Authorization;
using MobiRooz.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace MobiRooz.Application.Commands.Tickets;

public sealed record CreateTicketReplyCommand(
    Guid TicketId,
    string Message,
    bool IsFromAdmin,
    string? RepliedById = null) : ICommand<TicketReplyDto>
{
    public sealed class Handler : ICommandHandler<CreateTicketReplyCommand, TicketReplyDto>
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

        public async Task<Result<TicketReplyDto>> Handle(CreateTicketReplyCommand request, CancellationToken cancellationToken)
        {
            if (request.TicketId == Guid.Empty)
            {
                return Result<TicketReplyDto>.Failure("شناسه تیکت الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Result<TicketReplyDto>.Failure("متن پاسخ الزامی است.");
            }

            var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return Result<TicketReplyDto>.Failure("تیکت یافت نشد.");
            }

            var audit = _auditContext.Capture();
            var repliedById = request.RepliedById ?? audit.UserId;

            var reply = new TicketReply(
                request.TicketId,
                request.Message,
                request.IsFromAdmin,
                repliedById);

            reply.CreatorId = audit.UserId;
            reply.CreateDate = audit.Timestamp;
            reply.UpdateDate = audit.Timestamp;
            reply.Ip = audit.IpAddress;

            await _ticketRepository.AddReplyAsync(reply, cancellationToken);

            // Update ticket status and last reply date
            var now = DateTimeOffset.UtcNow;
            ticket.UpdateLastReplyDate(now);
            
            if (request.IsFromAdmin)
            {
                ticket.UpdateStatus(Domain.Enums.TicketStatus.Answered);
                ticket.MarkAsUnread(); // Mark as unread for user
            }
            else
            {
                ticket.UpdateStatus(Domain.Enums.TicketStatus.InProgress);
                ticket.MarkAsUnread(); // Mark as unread for admin
            }

            await _ticketRepository.UpdateAsync(ticket, cancellationToken);

            var replyWithUser = await _ticketRepository.GetByIdWithRepliesAsync(request.TicketId, cancellationToken);
            var createdReply = replyWithUser?.Replies.FirstOrDefault(r => r.Id == reply.Id);
            
            if (createdReply is null)
            {
                return Result<TicketReplyDto>.Failure("خطا در ایجاد پاسخ.");
            }

            var dto = new TicketReplyDto(
                createdReply.Id,
                createdReply.TicketId,
                createdReply.Message,
                createdReply.IsFromAdmin,
                createdReply.RepliedById,
                createdReply.RepliedBy?.FullName,
                createdReply.CreateDate);

            // Create notification
            try
            {
                if (request.IsFromAdmin)
                {
                    // Notify user about admin reply
                    var notification = new Notification(
                        "پاسخ به تیکت",
                        $"پاسخی به تیکت شما با موضوع «{ticket.Subject}» داده شد.",
                        NotificationType.System,
                        NotificationPriority.Normal,
                        null);

                    notification.CreatorId = audit.UserId;
                    notification.CreateDate = audit.Timestamp;
                    notification.UpdateDate = audit.Timestamp;
                    notification.Ip = audit.IpAddress;
                    notification.SetCreatedBy(audit.UserId);

                    await _notificationRepository.AddAsync(notification, cancellationToken);

                    var userNotification = new UserNotification(notification.Id, ticket.UserId);
                    userNotification.CreatorId = audit.UserId;
                    userNotification.CreateDate = audit.Timestamp;
                    userNotification.UpdateDate = audit.Timestamp;
                    userNotification.Ip = audit.IpAddress;

                    await _notificationRepository.AddUserNotificationAsync(userNotification, cancellationToken);
                }
                else
                {
                    // Notify ONLY admins about user reply (never send to regular users)
                    var adminUsers = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
                    if (adminUsers.Count > 0)
                    {
                        var notification = new Notification(
                            "پاسخ کاربر به تیکت",
                            $"کاربر {ticket.User?.FullName ?? ticket.User?.UserName ?? "نامشخص"} به تیکت «{ticket.Subject}» پاسخ داد.",
                            NotificationType.System,
                            NotificationPriority.Normal,
                            null);

                        notification.CreatorId = audit.UserId;
                        notification.CreateDate = audit.Timestamp;
                        notification.UpdateDate = audit.Timestamp;
                        notification.Ip = audit.IpAddress;
                        notification.SetCreatedBy(audit.UserId);

                        await _notificationRepository.AddAsync(notification, cancellationToken);

                        // IMPORTANT: Only send to active admin users, exclude the user who replied if they are an admin
                        // Regular users should NEVER receive these notifications
                        var userNotifications = adminUsers
                            .Where(u => u.IsActive 
                                && !u.IsDeleted 
                                && u.Id != repliedById 
                                && u.Id != ticket.UserId) // Also exclude ticket owner
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
            }
            catch
            {
                // Silently fail notification creation - don't break reply creation
            }

            return Result<TicketReplyDto>.Success(dto);
        }
    }
}
