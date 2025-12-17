using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Notifications;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Notifications;

public sealed record GetAdminCreatedNotificationsQuery(
    string AdminId,
    string? SearchTitle = null,
    string? SearchMessage = null,
    int? TypeFilter = null,
    int? PriorityFilter = null,
    DateTimeOffset? DateFromFilter = null,
    DateTimeOffset? DateToFilter = null,
    bool? IsActiveFilter = null) : IQuery<AdminNotificationsListDto>
{
    public sealed class Handler : IQueryHandler<GetAdminCreatedNotificationsQuery, AdminNotificationsListDto>
    {
        private readonly INotificationRepository _notificationRepository;

        public Handler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Result<AdminNotificationsListDto>> Handle(GetAdminCreatedNotificationsQuery request, CancellationToken cancellationToken)
        {
            var notifications = await _notificationRepository.GetAdminCreatedNotificationsAsync(
                request.AdminId,
                request.SearchTitle,
                request.SearchMessage,
                request.TypeFilter,
                request.PriorityFilter,
                request.DateFromFilter,
                request.DateToFilter,
                request.IsActiveFilter,
                cancellationToken);

            var dtos = notifications.Select(n => new AdminNotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.Priority,
                n.SentAt,
                n.ExpiresAt,
                n.IsActive,
                n.UserNotifications?.Count ?? 0))
                .ToList();

            var stats = new AdminNotificationStatsDto(
                Total: dtos.Count,
                Active: dtos.Count(n => n.IsActive),
                Inactive: dtos.Count(n => !n.IsActive),
                Expired: dtos.Count(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value < DateTimeOffset.UtcNow));

            return Result<AdminNotificationsListDto>.Success(
                new AdminNotificationsListDto(dtos, stats));
        }
    }
}
