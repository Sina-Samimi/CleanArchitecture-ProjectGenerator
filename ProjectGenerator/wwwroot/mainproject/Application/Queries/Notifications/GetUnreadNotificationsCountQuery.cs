using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.Interfaces;
using MobiRooz.SharedKernel.BaseTypes;

namespace MobiRooz.Application.Queries.Notifications;

public sealed record GetUnreadNotificationsCountQuery(string UserId, string? UserRole = null) : IQuery<int>
{
    public sealed class Handler : IQueryHandler<GetUnreadNotificationsCountQuery, int>
    {
        private readonly INotificationRepository _notificationRepository;

        public Handler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Result<int>> Handle(GetUnreadNotificationsCountQuery request, CancellationToken cancellationToken)
        {
            var count = await _notificationRepository.GetUnreadCountAsync(request.UserId, request.UserRole, cancellationToken);
            return Result<int>.Success(count);
        }
    }
}

