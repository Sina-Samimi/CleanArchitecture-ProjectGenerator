using System;
using System.Collections.Generic;
using System.Linq;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs;
using Attar.Domain.Entities;
using Attar.SharedKernel.BaseTypes;
using Attar.SharedKernel.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Attar.Application.Commands.Identity.DeactivateUser;

public sealed record DeactivateUserCommand(string UserId, string? Reason = null) : ICommand<UserDto>
{
    public sealed class Handler : ICommandHandler<DeactivateUserCommand, UserDto>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public Handler(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<UserDto>> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user is null || user.IsDeleted)
            {
                return Result<UserDto>.Failure($"User with id '{request.UserId}' was not found.");
            }

            if (!user.IsActive)
            {
                return Result<UserDto>.Failure($"User '{user.Email}' is already deactivated.");
            }

            user.IsActive = false;
            user.DeactivatedOn = DateTimeOffset.UtcNow;
            user.DeactivationReason = string.IsNullOrWhiteSpace(request.Reason)
                ? user.DeactivationReason
                : request.Reason.Trim();
            user.LastModifiedOn = DateTimeOffset.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var error = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                return Result<UserDto>.Failure(error);
            }

            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                var error = string.Join("; ", stampResult.Errors.Select(e => e.Description));
                return Result<UserDto>.Failure(error);
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
                var identityRole = await _roleManager.FindByNameAsync(roleName);
                if (identityRole is null)
                {
                    lookup[roleName] = roleName;
                    continue;
                }

                var claims = await _roleManager.GetClaimsAsync(identityRole);
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
