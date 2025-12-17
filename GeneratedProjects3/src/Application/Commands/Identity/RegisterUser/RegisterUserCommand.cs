using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs;
using LogTableRenameTest.Domain.Entities;
using LogTableRenameTest.SharedKernel.Authorization;
using LogTableRenameTest.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace LogTableRenameTest.Application.Commands.Identity.RegisterUser;

public sealed record RegisterUserCommand(RegisterUserDto Payload) : ICommand<UserDto>
{
    public sealed class Handler : ICommandHandler<RegisterUserCommand, UserDto>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public Handler(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Payload;
            var normalizedRoles = (dto.Roles ?? Array.Empty<string>())
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var now = DateTimeOffset.UtcNow;

            var user = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.UserName,
                PhoneNumber = dto.PhoneNumber,
                FullName = dto.FullName,
                IsActive = dto.IsActive,
                DeactivationReason = dto.IsActive ? null : dto.DeactivationReason,
                DeactivatedOn = dto.IsActive ? null : now,
                AvatarPath = dto.AvatarPath,
                CreatedOn = now,
                LastModifiedOn = now
            };

            var password = dto.Password;
            if (string.IsNullOrWhiteSpace(password))
            {
                // Generate a random password if not provided
                password = GenerateRandomPassword(12);
            }

            var createUserResult = await _userManager.CreateAsync(user, password);
            if (!createUserResult.Succeeded)
            {
                var error = string.Join("; ", createUserResult.Errors.Select(e => e.Description));
                return Result<UserDto>.Failure(error);
            }

            // If no roles are specified, assign the default "User" role
            if (normalizedRoles.Length == 0)
            {
                normalizedRoles = new[] { RoleNames.User };
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
                        var roleCreationError = string.Join("; ", createRoleResult.Errors.Select(e => e.Description));
                        return Result<UserDto>.Failure(roleCreationError);
                    }
                }

                var displayNameError = await EnsureRoleDisplayNameAsync(identityRole, role);
                if (displayNameError is not null)
                {
                    return Result<UserDto>.Failure(displayNameError);
                }

                var addToRoleResult = await _userManager.AddToRoleAsync(user, role);
                if (!addToRoleResult.Succeeded)
                {
                    var addToRoleError = string.Join("; ", addToRoleResult.Errors.Select(e => e.Description));
                    return Result<UserDto>.Failure(addToRoleError);
                }
            }

            var roles = normalizedRoles;

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
                    return string.Join("; ", addResult.Errors.Select(error => error.Description));
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

        private static string GenerateRandomPassword(int length)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var result = new StringBuilder(length);
            foreach (var b in randomBytes)
            {
                result.Append(validChars[b % validChars.Length]);
            }

            return result.ToString();
        }
    }
}
