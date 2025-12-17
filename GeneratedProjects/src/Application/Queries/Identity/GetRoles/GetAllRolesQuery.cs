using System.Collections.Generic;
using System.Linq;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs;
using TestAttarClone.SharedKernel.Authorization;
using TestAttarClone.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TestAttarClone.Application.Queries.Identity.GetRoles;

public sealed record GetAllRolesQuery : IQuery<IReadOnlyCollection<RoleSummaryDto>>;

public sealed class GetAllRolesQueryHandler : IQueryHandler<GetAllRolesQuery, IReadOnlyCollection<RoleSummaryDto>>
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public GetAllRolesQueryHandler(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<Result<IReadOnlyCollection<RoleSummaryDto>>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleManager.Roles
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        var summaries = new List<RoleSummaryDto>(roles.Count);

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var displayNameClaim = claims.FirstOrDefault(claim =>
                string.Equals(claim.Type, RoleClaimTypes.DisplayName, StringComparison.OrdinalIgnoreCase));
            var displayName = !string.IsNullOrWhiteSpace(displayNameClaim?.Value)
                ? displayNameClaim!.Value
                : role.Name ?? string.Empty;

            summaries.Add(new RoleSummaryDto(
                role.Id,
                role.Name ?? string.Empty,
                displayName));
        }

        return Result<IReadOnlyCollection<RoleSummaryDto>>.Success(summaries);
    }
}
