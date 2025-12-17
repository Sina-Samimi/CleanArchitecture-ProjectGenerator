using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities;
using LogsDtoCloneTest.SharedKernel.Authorization;
using LogsDtoCloneTest.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;

namespace LogsDtoCloneTest.Application.Queries.Identity.GetRoles;

public sealed record GetRoleAccessLevelByIdQuery(string Id) : IQuery<RoleAccessLevelDto>;

public sealed class GetRoleAccessLevelByIdQueryHandler : IQueryHandler<GetRoleAccessLevelByIdQuery, RoleAccessLevelDto>
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionDefinitionService _permissionDefinitionService;

    public GetRoleAccessLevelByIdQueryHandler(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IPermissionDefinitionService permissionDefinitionService)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _permissionDefinitionService = permissionDefinitionService;
    }

    public async Task<Result<RoleAccessLevelDto>> Handle(GetRoleAccessLevelByIdQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            return Result<RoleAccessLevelDto>.Failure("شناسه نقش نامعتبر است.");
        }

        var role = await _roleManager.FindByIdAsync(request.Id);
        if (role is null)
        {
            return Result<RoleAccessLevelDto>.Failure("نقش مورد نظر یافت نشد.");
        }

        var validKeys = await _permissionDefinitionService.GetAllKeysAsync(cancellationToken);

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

        var dto = new RoleAccessLevelDto(
            role.Id,
            role.Name ?? string.Empty,
            displayName,
            permissions,
            usersInRole.Count);

        return Result<RoleAccessLevelDto>.Success(dto);
    }
}
