using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities;
using TestAttarClone.SharedKernel.Authorization;
using TestAttarClone.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TestAttarClone.Application.Queries.Identity.GetRoles;

public sealed record GetRoleAccessLevelsQuery : IQuery<IReadOnlyCollection<RoleAccessLevelDto>>;

public sealed class GetRoleAccessLevelsQueryHandler : IQueryHandler<GetRoleAccessLevelsQuery, IReadOnlyCollection<RoleAccessLevelDto>>
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionDefinitionService _permissionDefinitionService;

    public GetRoleAccessLevelsQueryHandler(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IPermissionDefinitionService permissionDefinitionService)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _permissionDefinitionService = permissionDefinitionService;
    }

    public async Task<Result<IReadOnlyCollection<RoleAccessLevelDto>>> Handle(GetRoleAccessLevelsQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleManager.Roles
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        var accessLevels = new List<RoleAccessLevelDto>(roles.Count);

        var validKeys = await _permissionDefinitionService.GetAllKeysAsync(cancellationToken);

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var displayNameClaim = claims.FirstOrDefault(claim =>
                string.Equals(claim.Type, RoleClaimTypes.DisplayName, StringComparison.OrdinalIgnoreCase));
            var displayName = !string.IsNullOrWhiteSpace(displayNameClaim?.Value)
                ? displayNameClaim!.Value
                : role.Name ?? string.Empty;
            var permissions = claims
                .Where(claim => string.Equals(claim.Type, PermissionCatalog.ClaimType, StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value)
                .Where(permission => validKeys.Contains(permission))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var usersInRole = role.Name is null
                ? Array.Empty<ApplicationUser>()
                : await _userManager.GetUsersInRoleAsync(role.Name);

            accessLevels.Add(new RoleAccessLevelDto(
                role.Id,
                role.Name ?? string.Empty,
                displayName,
                permissions,
                usersInRole.Count));
        }

        return Result<IReadOnlyCollection<RoleAccessLevelDto>>.Success(accessLevels.ToArray());
    }
}
