using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Commands.Notifications;

public sealed record DeleteNotificationCommand(Guid NotificationId) : ICommand
{
    public sealed class Handler : ICommandHandler<DeleteNotificationCommand>
    {
        private readonly INotificationRepository _notificationRepository;

        public Handler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Result> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
            if (notification == null)
            {
                return Result.Failure("اعلان یافت نشد.");
            }

            await _notificationRepository.DeleteAsync(request.NotificationId, cancellationToken);
            return Result.Success();
        }
    }
}
