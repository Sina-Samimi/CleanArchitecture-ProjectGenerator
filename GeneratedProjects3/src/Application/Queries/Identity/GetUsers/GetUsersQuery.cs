using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities;
using LogTableRenameTest.SharedKernel.Authorization;
using LogTableRenameTest.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogTableRenameTest.Application.Queries.Identity.GetUsers;

public enum UserStatusFilter
{
    All,
    Active,
    Inactive,
    Deleted
}

public sealed record GetUsersQuery(UserFilterCriteria Filters) : IQuery<IReadOnlyCollection<UserDto>>
{
    public sealed class Handler : IQueryHandler<GetUsersQuery, IReadOnlyCollection<UserDto>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserSessionRepository _userSessionRepository;
        private const int OnlineThresholdMinutes = 5;

        public Handler(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IUserSessionRepository userSessionRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userSessionRepository = userSessionRepository;
        }

        public async Task<Result<IReadOnlyCollection<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            usersQuery = ApplyStatusFilters(usersQuery, request.Filters);

            if (!string.IsNullOrWhiteSpace(request.Filters.FullName))
            {
                var pattern = $"%{request.Filters.FullName.Trim()}%";
                usersQuery = usersQuery.Where(user => EF.Functions.Like(user.FullName, pattern));
            }

            if (!string.IsNullOrWhiteSpace(request.Filters.PhoneNumber))
            {
                var pattern = $"%{request.Filters.PhoneNumber}%";
                usersQuery = usersQuery.Where(user =>
                    user.PhoneNumber != null &&
                    EF.Functions.Like(
                        user.PhoneNumber
                            .Replace(" ", string.Empty)
                            .Replace("-", string.Empty)
                            .Replace("+", string.Empty)
                            .Replace("(", string.Empty)
                            .Replace(")", string.Empty),
                        pattern));
            }

            if (request.Filters.RegisteredFrom.HasValue)
            {
                usersQuery = usersQuery.Where(user => user.CreatedOn >= request.Filters.RegisteredFrom.Value);
            }

            if (request.Filters.RegisteredTo.HasValue)
            {
                usersQuery = usersQuery.Where(user => user.CreatedOn < request.Filters.RegisteredTo.Value);
            }

            var users = await usersQuery
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.Email)
                .ToListAsync(cancellationToken);

            var dtos = new List<UserDto>(users.Count);
            var roleDisplayLookup = await BuildRoleDisplayLookupAsync(cancellationToken);
            
            // Get user IDs for batch session lookup
            var userIds = users.Select(u => u.Id).ToList();
            
            // Get last seen times for all users in one query
            var lastSeenTimes = await _userSessionRepository.GetLastSeenTimesAsync(userIds, cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var onlineThreshold = TimeSpan.FromMinutes(OnlineThresholdMinutes);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (!string.IsNullOrWhiteSpace(request.Filters.Role))
                {
                    var hasRole = roles.Any(role => string.Equals(role, request.Filters.Role, StringComparison.OrdinalIgnoreCase));
                    if (!hasRole)
                    {
                        continue;
                    }
                }

                var roleMemberships = roles
                    .Select(role => new RoleMembershipDto(
                        role,
                        roleDisplayLookup.TryGetValue(role, out var display)
                            ? display
                            : role))
                    .ToArray();

                // Check if user is online
                var lastSeenAt = lastSeenTimes.TryGetValue(user.Id, out var lastSeen) ? lastSeen : (DateTimeOffset?)null;
                var isOnline = lastSeenAt.HasValue && (now - lastSeenAt.Value) <= onlineThreshold;

                dtos.Add(new UserDto(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.FullName,
                    user.IsActive,
                    user.IsDeleted,
                    user.DeactivatedOn,
                    user.DeletedOn,
                    user.PhoneNumber ?? string.Empty,
                    user.AvatarPath,
                    roleMemberships,
                    user.LastModifiedOn,
                    isOnline,
                    lastSeenAt));
            }

            return Result<IReadOnlyCollection<UserDto>>.Success(dtos);
        }

        private async Task<Dictionary<string, string>> BuildRoleDisplayLookupAsync(CancellationToken cancellationToken)
        {
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var roles = await _roleManager.Roles
                .OrderBy(role => role.Name)
                .ToListAsync(cancellationToken);

            foreach (var role in roles)
            {
                if (string.IsNullOrWhiteSpace(role.Name))
                {
                    continue;
                }

                var claims = await _roleManager.GetClaimsAsync(role);
                var displayNameClaim = claims.FirstOrDefault(claim =>
                    string.Equals(claim.Type, RoleClaimTypes.DisplayName, StringComparison.OrdinalIgnoreCase));
                var displayName = !string.IsNullOrWhiteSpace(displayNameClaim?.Value)
                    ? displayNameClaim!.Value
                    : role.Name;

                lookup[role.Name] = displayName;
            }

            return lookup;
        }

        private static IQueryable<ApplicationUser> ApplyStatusFilters(IQueryable<ApplicationUser> query, UserFilterCriteria filters)
        {
            if (!filters.IncludeDeleted)
            {
                query = query.Where(user => !user.IsDeleted);
            }

            if (!filters.IncludeDeactivated)
            {
                query = query.Where(user => user.IsActive);
            }

            return filters.Status switch
            {
                UserStatusFilter.Active => query.Where(user => user.IsActive && !user.IsDeleted),
                UserStatusFilter.Inactive => query.Where(user => !user.IsActive && !user.IsDeleted),
                UserStatusFilter.Deleted => query.Where(user => user.IsDeleted),
                _ => query
            };
        }
    }
}
