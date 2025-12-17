using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs;
using Attar.Application.Interfaces;
using Attar.Domain.Entities;
using Attar.SharedKernel.Authorization;
using Attar.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace Attar.Application.Queries.Identity.GetUserById;

public sealed record GetUserByIdQuery(string UserId) : IQuery<UserDto>
{
    public sealed class Handler : IQueryHandler<GetUserByIdQuery, UserDto>
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

        public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user is null || user.IsDeleted)
            {
                return Result<UserDto>.Failure($"User with id '{request.UserId}' was not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var roleMemberships = await MapRolesAsync(roles);
            
            // Get last seen time for user
            var userIds = new[] { user.Id };
            var lastSeenTimes = await _userSessionRepository.GetLastSeenTimesAsync(userIds, cancellationToken);
            var lastSeenAt = lastSeenTimes.TryGetValue(user.Id, out var lastSeen) ? lastSeen : (DateTimeOffset?)null;
            
            // Check if user is online
            var now = DateTimeOffset.UtcNow;
            var onlineThreshold = TimeSpan.FromMinutes(OnlineThresholdMinutes);
            var isOnline = lastSeenAt.HasValue && (now - lastSeenAt.Value) <= onlineThreshold;
            
            var dto = new UserDto(
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
                lastSeenAt);

            return Result<UserDto>.Success(dto);
        }

        private async Task<IReadOnlyCollection<RoleMembershipDto>> MapRolesAsync(IEnumerable<string> roles)
        {
            var roleList = roles.Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => role.Trim()).ToArray();
            if (roleList.Length == 0)
            {
                return Array.Empty<RoleMembershipDto>();
            }

            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var roleName in roleList)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role is null)
                {
                    lookup[roleName] = roleName;
                    continue;
                }

                var claims = await _roleManager.GetClaimsAsync(role);
                var displayNameClaim = claims.FirstOrDefault(claim =>
                    string.Equals(claim.Type, RoleClaimTypes.DisplayName, StringComparison.OrdinalIgnoreCase));
                var displayName = !string.IsNullOrWhiteSpace(displayNameClaim?.Value)
                    ? displayNameClaim!.Value
                    : roleName;

                lookup[roleName] = displayName;
            }

            return roleList
                .Select(roleName => new RoleMembershipDto(
                    roleName,
                    lookup.TryGetValue(roleName, out var display)
                        ? display
                        : roleName))
                .ToArray();
        }
    }
}
