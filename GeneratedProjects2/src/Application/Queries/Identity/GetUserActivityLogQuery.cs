using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Identity;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities;
using LogsDtoCloneTest.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace LogsDtoCloneTest.Application.Queries.Identity;

public sealed record GetUserActivityLogQuery(
    string UserId, 
    Guid? CurrentSessionId = null,
    int PageNumber = 1, 
    int PageSize = 20,
    string? ActivityType = null,
    string? DeviceType = null,
    bool? IsActive = null) : IQuery<ActivityLogResultDto>;

public sealed class GetUserActivityLogQueryHandler : IQueryHandler<GetUserActivityLogQuery, ActivityLogResultDto>
{
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUserActivityLogQueryHandler(
        IUserSessionRepository userSessionRepository,
        UserManager<ApplicationUser> userManager)
    {
        _userSessionRepository = userSessionRepository;
        _userManager = userManager;
    }

    public async Task<Result<ActivityLogResultDto>> Handle(GetUserActivityLogQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<ActivityLogResultDto>.Failure("شناسه کاربر معتبر نیست.");
        }

        if (request.PageNumber < 1)
        {
            return Result<ActivityLogResultDto>.Failure("شماره صفحه باید بزرگتر از 0 باشد.");
        }

        if (request.PageSize < 1 || request.PageSize > 100)
        {
            return Result<ActivityLogResultDto>.Failure("تعداد آیتم‌ها در هر صفحه باید بین 1 تا 100 باشد.");
        }

        // Get user sessions
        var sessions = await _userSessionRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        
        // Get user info for registration date
        var user = await _userManager.FindByIdAsync(request.UserId);
        var registrationDate = user?.CreatedOn ?? DateTimeOffset.UtcNow;
        var lastModifiedDate = user?.LastModifiedOn ?? DateTimeOffset.UtcNow;

        var activities = new List<ActivityEntryDto>();

        // Add registration activity
        activities.Add(new ActivityEntryDto(
            SessionId: null,
            Title: "تکمیل ثبت‌نام",
            Timestamp: registrationDate,
            Context: "سیستم",
            DeviceType: null,
            ClientName: null,
            IpAddress: null,
            IsCurrentSession: false,
            IsActive: false));

        // Add profile update activity (if different from registration)
        if (lastModifiedDate > registrationDate.AddMinutes(1))
        {
            activities.Add(new ActivityEntryDto(
                SessionId: null,
                Title: "بروزرسانی پروفایل",
                Timestamp: lastModifiedDate,
                Context: "سیستم",
                DeviceType: null,
                ClientName: null,
                IpAddress: null,
                IsCurrentSession: false,
                IsActive: false));
        }

        // Add login sessions (exclude deleted sessions)
        foreach (var session in sessions.Where(s => !s.IsDeleted).OrderByDescending(s => s.SignedInAt))
        {
            var isActive = session.SignedOutAt == null;
            var context = $"{session.DeviceType} - {session.ClientName}";
            
            // Identify current session by matching session ID from cookie
            var isCurrentSession = isActive && 
                request.CurrentSessionId.HasValue &&
                session.Id == request.CurrentSessionId.Value;
            
            activities.Add(new ActivityEntryDto(
                SessionId: session.Id,
                Title: isActive ? "ورود به سیستم" : "خروج از سیستم",
                Timestamp: isActive ? session.SignedInAt : session.SignedOutAt!.Value,
                Context: context,
                DeviceType: session.DeviceType,
                ClientName: session.ClientName,
                IpAddress: session.Ip?.ToString(),
                IsCurrentSession: isCurrentSession,
                IsActive: isActive));
        }

        // Apply filters
        var filteredActivities = activities.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.ActivityType))
        {
            filteredActivities = filteredActivities.Where(a => 
                a.Title.Contains(request.ActivityType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceType))
        {
            filteredActivities = filteredActivities.Where(a => 
                !string.IsNullOrWhiteSpace(a.DeviceType) &&
                a.DeviceType.Contains(request.DeviceType, StringComparison.OrdinalIgnoreCase));
        }

        if (request.IsActive.HasValue)
        {
            filteredActivities = filteredActivities.Where(a => a.IsActive == request.IsActive.Value);
        }

        // Sort by timestamp descending
        var sortedActivities = filteredActivities
            .OrderByDescending(a => a.Timestamp)
            .ToList();

        var totalCount = sortedActivities.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var pageNumber = Math.Min(request.PageNumber, Math.Max(1, totalPages));

        // Apply pagination
        var pagedActivities = sortedActivities
            .Skip((pageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var result = new ActivityLogResultDto(
            Items: pagedActivities,
            TotalCount: totalCount,
            PageNumber: pageNumber,
            PageSize: request.PageSize,
            TotalPages: totalPages);

        return Result<ActivityLogResultDto>.Success(result);
    }
}

