using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using MobiRooz.Application.Abstractions.Messaging;
using MobiRooz.Application.DTOs;
using MobiRooz.Domain.Entities;
using MobiRooz.SharedKernel.BaseTypes;
using MobiRooz.SharedKernel.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MobiRooz.Application.Commands.Identity.UpdateUser;

public sealed record UpdateUserCommand(UpdateUserDto Payload) : ICommand<UserDto>
{
    public sealed class Handler : ICommandHandler<UpdateUserCommand, UserDto>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public Handler(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Payload;

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user is null || user.IsDeleted)
            {
                return Result<UserDto>.Failure($"User with id '{dto.UserId}' was not found.");
            }

            var hasMutations = false;
            var normalizedRoles = Array.Empty<string>();
            var rolesToRemove = Array.Empty<string>();
            var rolesToAdd = Array.Empty<string>();
            var rolesChanged = false;

            var trimmedEmail = dto.Email?.Trim();
            if (dto.Email is not null)
            {
                if (string.IsNullOrWhiteSpace(trimmedEmail))
                {
                    if (!string.IsNullOrWhiteSpace(user.Email))
                    {
                        user.Email = null;
                        user.NormalizedEmail = null;
                        hasMutations = true;
                    }
                }
                else if (!string.Equals(trimmedEmail, user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var normalizedEmail = _userManager.NormalizeEmail(trimmedEmail);
                    var existingUser = await _userManager.Users
                        .Where(candidate => candidate.NormalizedEmail == normalizedEmail && candidate.Id != user.Id && !candidate.IsDeleted)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existingUser is not null)
                    {
                        return Result<UserDto>.Failure($"Email '{trimmedEmail}' is already in use.");
                    }

                    user.Email = trimmedEmail;
                    user.NormalizedEmail = normalizedEmail;
                    hasMutations = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.FullName) && !string.Equals(dto.FullName, user.FullName, StringComparison.Ordinal))
            {
                user.FullName = dto.FullName;
                hasMutations = true;
            }

            if (dto.IsActive.HasValue)
            {
                if (dto.IsActive.Value)
                {
                    var wasInactive = !user.IsActive;
                    var hadDeactivationMetadata = user.DeactivatedOn is not null || user.DeactivationReason is not null;

                    if (wasInactive)
                    {
                        user.IsActive = true;
                    }

                    if (hadDeactivationMetadata)
                    {
                        user.DeactivatedOn = null;
                        user.DeactivationReason = null;
                    }

                    if (wasInactive || hadDeactivationMetadata)
                    {
                        hasMutations = true;
                    }
                }
                else if (user.IsActive)
                {
                    user.IsActive = false;
                    user.DeactivatedOn = DateTimeOffset.UtcNow;
                    hasMutations = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && !string.Equals(dto.PhoneNumber, user.PhoneNumber, StringComparison.Ordinal))
            {
                user.PhoneNumber = dto.PhoneNumber;
                hasMutations = true;
            }

            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                var expectedUserName = BuildPhoneUserName(user.PhoneNumber);
                if (!string.Equals(user.UserName, expectedUserName, StringComparison.OrdinalIgnoreCase))
                {
                    user.UserName = expectedUserName;
                    user.NormalizedUserName = _userManager.NormalizeName(expectedUserName);
                    hasMutations = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.AvatarPath))
            {
                user.AvatarPath = dto.AvatarPath;
                hasMutations = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, dto.Password);
                if (!resetResult.Succeeded)
                {
                    return Result<UserDto>.Failure(CombineErrors(resetResult));
                }
            }

            if (dto.Roles is not null)
            {
                normalizedRoles = dto.Roles
                    .Where(role => !string.IsNullOrWhiteSpace(role))
                    .Select(role => role.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var currentRoles = await _userManager.GetRolesAsync(user);

                rolesToRemove = currentRoles
                    .Except(normalizedRoles, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                rolesToAdd = normalizedRoles
                    .Except(currentRoles, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                rolesChanged = rolesToRemove.Length > 0 || rolesToAdd.Length > 0;
            }

            if (hasMutations || rolesToRemove.Length > 0 || rolesToAdd.Length > 0)
            {
                user.LastModifiedOn = DateTimeOffset.UtcNow;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return Result<UserDto>.Failure(CombineErrors(updateResult));
            }

            if (dto.Roles is not null)
            {
                if (rolesToRemove.Length > 0)
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    if (!removeResult.Succeeded)
                    {
                        return Result<UserDto>.Failure(CombineErrors(removeResult));
                    }
                }

                foreach (var role in normalizedRoles)
                {
                    var identityRole = await _roleManager.FindByNameAsync(role);
                    if (identityRole is null)
                    {
                        identityRole = new IdentityRole(role);
                        var createRoleResult = await _roleManager.CreateAsync(identityRole);
                        if (!createRoleResult.Succeeded)
                        {
                            return Result<UserDto>.Failure(CombineErrors(createRoleResult));
                        }
                    }

                    var displayNameError = await EnsureRoleDisplayNameAsync(identityRole, role);
                    if (displayNameError is not null)
                    {
                        return Result<UserDto>.Failure(displayNameError);
                    }
                }

                if (rolesToAdd.Length > 0)
                {
                    var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                    if (!addResult.Succeeded)
                    {
                        return Result<UserDto>.Failure(CombineErrors(addResult));
                    }
                }

                if (rolesChanged)
                {
                    var securityStampResult = await _userManager.UpdateSecurityStampAsync(user);
                    if (!securityStampResult.Succeeded)
                    {
                        return Result<UserDto>.Failure(CombineErrors(securityStampResult));
                    }
                }
            }

            var roles = await _userManager.GetRolesAsync(user);
            var roleMemberships = await MapRolesAsync(roles);
            var result = new UserDto(
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

            return Result<UserDto>.Success(result);
        }

        private static string CombineErrors(IdentityResult result)
            => string.Join("; ", result.Errors.Select(error => error.Description));

        private static string BuildPhoneUserName(string phoneNumber)
            => string.Concat(phoneNumber, "@gmail.com");

        private async Task<string?> EnsureRoleDisplayNameAsync(IdentityRole role, string fallback)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var displayNameClaim = claims.FirstOrDefault(claim =>
                string.Equals(claim.Type, RoleClaimTypes.DisplayName, StringComparison.OrdinalIgnoreCase));

            if (displayNameClaim is null)
            {
                var addResult = await _roleManager.AddClaimAsync(role, new Claim(RoleClaimTypes.DisplayName, fallback));
                if (!addResult.Succeeded)
                {
                    return CombineErrors(addResult);
                }
            }

            return null;
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
