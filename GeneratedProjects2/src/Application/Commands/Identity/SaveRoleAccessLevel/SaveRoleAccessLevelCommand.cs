using System.Collections.Generic;
using System.Security.Claims;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities;
using LogsDtoCloneTest.SharedKernel.Authorization;
using LogsDtoCloneTest.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace LogsDtoCloneTest.Application.Commands.Identity.SaveRoleAccessLevel;

public sealed record SaveRoleAccessLevelCommand(SaveRoleAccessLevelDto Payload) : ICommand<RoleAccessLevelDto>;

public sealed record SaveRoleAccessLevelDto(string? Id, string Name, string DisplayName, IReadOnlyCollection<string> Permissions);

public sealed class SaveRoleAccessLevelCommandHandler : ICommandHandler<SaveRoleAccessLevelCommand, RoleAccessLevelDto>
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionDefinitionService _permissionDefinitionService;

    public SaveRoleAccessLevelCommandHandler(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IPermissionDefinitionService permissionDefinitionService)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _permissionDefinitionService = permissionDefinitionService;
    }

    public async Task<Result<RoleAccessLevelDto>> Handle(SaveRoleAccessLevelCommand request, CancellationToken cancellationToken)
    {
        var payload = request.Payload;
        var trimmedName = payload.Name?.Trim();
        var trimmedDisplayName = payload.DisplayName?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return Result<RoleAccessLevelDto>.Failure("نام نقش نمی‌تواند خالی باشد.");
        }

        if (string.IsNullOrWhiteSpace(trimmedDisplayName))
        {
            return Result<RoleAccessLevelDto>.Failure("عنوان فارسی نقش نمی‌تواند خالی باشد.");
        }

        IdentityRole? role = null;
        if (!string.IsNullOrWhiteSpace(payload.Id))
        {
            role = await _roleManager.FindByIdAsync(payload.Id);
            if (role is null)
            {
                return Result<RoleAccessLevelDto>.Failure("نقش مورد نظر یافت نشد.");
            }
        }

        var existingWithName = await _roleManager.FindByNameAsync(trimmedName);
        if (existingWithName is not null && (role is null || !string.Equals(existingWithName.Id, role.Id, StringComparison.Ordinal)))
        {
            return Result<RoleAccessLevelDto>.Failure("نقشی با این عنوان از قبل وجود دارد.");
        }

        if (role is null)
        {
            role = new IdentityRole(trimmedName);
            var createResult = await _roleManager.CreateAsync(role);
            if (!createResult.Succeeded)
            {
                return Result<RoleAccessLevelDto>.Failure(CombineErrors(createResult));
            }
        }
        else if (!string.Equals(role.Name, trimmedName, StringComparison.Ordinal))
        {
            role.Name = trimmedName;
            role.NormalizedName = trimmedName.ToUpperInvariant();
            var updateResult = await _roleManager.UpdateAsync(role);
            if (!updateResult.Succeeded)
            {
                return Result<RoleAccessLevelDto>.Failure(CombineErrors(updateResult));
            }
        }

        var validKeys = await _permissionDefinitionService.GetAllKeysAsync(cancellationToken);

        var normalizedPermissions = (payload.Permissions ?? Array.Empty<string>())
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission.Trim())
            .Where(permission => validKeys.Contains(permission))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var existingClaims = await _roleManager.GetClaimsAsync(role);

        var displayNameClaim = existingClaims.FirstOrDefault(claim =>
            string.Equals(claim.Type, RoleClaimTypes.DisplayName, StringComparison.OrdinalIgnoreCase));

        if (displayNameClaim is null)
        {
            var addDisplayName = await _roleManager.AddClaimAsync(role, new Claim(RoleClaimTypes.DisplayName, trimmedDisplayName));
            if (!addDisplayName.Succeeded)
            {
                return Result<RoleAccessLevelDto>.Failure(CombineErrors(addDisplayName));
            }
        }
        else if (!string.Equals(displayNameClaim.Value, trimmedDisplayName, StringComparison.Ordinal))
        {
            var removeDisplay = await _roleManager.RemoveClaimAsync(role, displayNameClaim);
            if (!removeDisplay.Succeeded)
            {
                return Result<RoleAccessLevelDto>.Failure(CombineErrors(removeDisplay));
            }

            var addDisplay = await _roleManager.AddClaimAsync(role, new Claim(RoleClaimTypes.DisplayName, trimmedDisplayName));
            if (!addDisplay.Succeeded)
            {
                return Result<RoleAccessLevelDto>.Failure(CombineErrors(addDisplay));
            }
        }

        var permissionClaims = existingClaims
            .Where(claim => string.Equals(claim.Type, PermissionCatalog.ClaimType, StringComparison.OrdinalIgnoreCase))
            .Where(claim => validKeys.Contains(claim.Value))
            .ToArray();

        var orphanedClaims = existingClaims
            .Where(claim => string.Equals(claim.Type, PermissionCatalog.ClaimType, StringComparison.OrdinalIgnoreCase))
            .Where(claim => !validKeys.Contains(claim.Value))
            .ToArray();

        foreach (var claim in orphanedClaims)
        {
            var removeInvalid = await _roleManager.RemoveClaimAsync(role, claim);
            if (!removeInvalid.Succeeded)
            {
                return Result<RoleAccessLevelDto>.Failure(CombineErrors(removeInvalid));
            }
        }

        foreach (var claim in permissionClaims)
        {
            if (!normalizedPermissions.Contains(claim.Value, StringComparer.OrdinalIgnoreCase))
            {
                var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
                if (!removeResult.Succeeded)
                {
                    return Result<RoleAccessLevelDto>.Failure(CombineErrors(removeResult));
                }
            }
        }

        foreach (var permission in normalizedPermissions)
        {
            var exists = permissionClaims.Any(claim => string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                var addResult = await _roleManager.AddClaimAsync(role, new Claim(PermissionCatalog.ClaimType, permission));
                if (!addResult.Succeeded)
                {
                    return Result<RoleAccessLevelDto>.Failure(CombineErrors(addResult));
                }
            }
        }

        var usersInRole = role.Name is null
            ? Array.Empty<ApplicationUser>()
            : await _userManager.GetUsersInRoleAsync(role.Name);

        var dto = new RoleAccessLevelDto(
            role.Id,
            role.Name ?? string.Empty,
            trimmedDisplayName,
            normalizedPermissions,
            usersInRole.Count);

        return Result<RoleAccessLevelDto>.Success(dto);
    }

    private static string CombineErrors(IdentityResult result)
        => string.Join("؛ ", result.Errors.Select(error => error.Description));
}
