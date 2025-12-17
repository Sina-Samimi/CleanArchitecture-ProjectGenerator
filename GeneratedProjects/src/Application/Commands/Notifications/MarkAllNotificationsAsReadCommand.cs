using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Notifications;

public sealed record MarkAllNotificationsAsReadCommand(string UserId) : ICommand
{
    public sealed class Handler : ICommandHandler<MarkAllNotificationsAsReadCommand>
    {
        private readonly INotificationRepository _notificationRepository;

        public Handler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Result> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
        {
            var unreadNotifications = await _notificationRepository.GetUserNotificationsAsync(
                request.UserId,
                isRead: false,
                cancellationToken);

            foreach (var userNotification in unreadNotifications)
            {
                userNotification.MarkAsRead();
                await _notificationRepository.UpdateUserNotificationAsync(userNotification, cancellationToken);
            }

            return Result.Success();
        }
    }
}

