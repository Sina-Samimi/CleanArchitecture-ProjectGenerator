using System;
using System.Collections.Generic;
using System.Linq;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs;
using Arsis.Domain.Entities;
using Arsis.SharedKernel.Authorization;
using Arsis.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace Arsis.Application.Queries.Identity.GetUserById;

public sealed record GetUserByIdQuery(string UserId) : IQuery<UserDto>
{
    public sealed class Handler : IQueryHandler<GetUserByIdQuery, UserDto>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public Handler(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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
                user.LastModifiedOn);

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
