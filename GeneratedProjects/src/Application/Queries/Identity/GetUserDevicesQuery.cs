using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs.Identity;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace TestAttarClone.Application.Queries.Identity;

public sealed record GetUserDevicesQuery(
    string UserId,
    int PageNumber = 1,
    int PageSize = 20,
    string? DeviceType = null,
    bool? IsActive = null) : IQuery<DeviceActivityResultDto>;

public sealed class GetUserDevicesQueryHandler : IQueryHandler<GetUserDevicesQuery, DeviceActivityResultDto>
{
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly UserManager<Domain.Entities.ApplicationUser> _userManager;

    public GetUserDevicesQueryHandler(
        IUserSessionRepository userSessionRepository,
        UserManager<Domain.Entities.ApplicationUser> userManager)
    {
        _userSessionRepository = userSessionRepository;
        _userManager = userManager;
    }

    public async Task<Result<DeviceActivityResultDto>> Handle(GetUserDevicesQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<DeviceActivityResultDto>.Failure("شناسه کاربر معتبر نیست.");
        }

        if (request.PageNumber < 1)
        {
            return Result<DeviceActivityResultDto>.Failure("شماره صفحه باید بزرگتر از 0 باشد.");
        }

        if (request.PageSize < 1 || request.PageSize > 100)
        {
            return Result<DeviceActivityResultDto>.Failure("تعداد آیتم‌ها در هر صفحه باید بین 1 تا 100 باشد.");
        }

        // Get user sessions
        var sessions = await _userSessionRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        
        // Get user info for registration date
        var user = await _userManager.FindByIdAsync(request.UserId);
        var registrationDate = user?.CreatedOn ?? DateTimeOffset.UtcNow;
        var lastModifiedDate = user?.LastModifiedOn ?? DateTimeOffset.UtcNow;

        // Group sessions by device (DeviceType + ClientName)
        var deviceGroups = sessions
            .Where(s => !s.IsDeleted && !string.IsNullOrWhiteSpace(s.DeviceType) && !string.IsNullOrWhiteSpace(s.ClientName))
            .GroupBy(s => $"{s.DeviceType}|{s.ClientName}")
            .Select(g =>
            {
                var deviceSessions = g.OrderByDescending(s => s.SignedInAt).ToList();
                var activeSessions = deviceSessions.Where(s => s.SignedOutAt == null).ToList();
                var firstSession = deviceSessions.Last();
                var lastSession = deviceSessions.First();
                
                return new DeviceActivityDto(
                    DeviceKey: g.Key,
                    DeviceType: firstSession.DeviceType,
                    ClientName: firstSession.ClientName,
                    IpAddress: lastSession.Ip?.ToString(),
                    FirstSeenAt: firstSession.SignedInAt,
                    LastSeenAt: lastSession.LastSeenAt,
                    IsActive: activeSessions.Any(),
                    ActiveSessionCount: activeSessions.Count,
                    TotalSessionCount: deviceSessions.Count);
            })
            .ToList();

        // Apply filters
        var filteredDevices = deviceGroups.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.DeviceType))
        {
            filteredDevices = filteredDevices.Where(d => 
                d.DeviceType.Contains(request.DeviceType, StringComparison.OrdinalIgnoreCase));
        }

        if (request.IsActive.HasValue)
        {
            filteredDevices = filteredDevices.Where(d => d.IsActive == request.IsActive.Value);
        }

        // Sort by LastSeenAt descending
        var sortedDevices = filteredDevices
            .OrderByDescending(d => d.LastSeenAt)
            .ToList();

        var totalCount = sortedDevices.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var pageNumber = Math.Min(request.PageNumber, Math.Max(1, totalPages));

        // Apply pagination
        var pagedDevices = sortedDevices
            .Skip((pageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var result = new DeviceActivityResultDto(
            Items: pagedDevices,
            TotalCount: totalCount,
            PageNumber: pageNumber,
            PageSize: request.PageSize,
            TotalPages: totalPages);

        return Result<DeviceActivityResultDto>.Success(result);
    }
}

