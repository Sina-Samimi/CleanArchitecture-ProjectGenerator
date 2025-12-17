using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.Interfaces;
using Attar.SharedKernel.Authorization;
using Attar.SharedKernel.BaseTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Attar.Application.Commands.Identity.Permissions;

public sealed record DeletePermissionCommand(Guid Id) : ICommand;

public sealed class DeletePermissionCommandHandler : ICommandHandler<DeletePermissionCommand>
{
    private readonly IAccessPermissionRepository _permissionRepository;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DeletePermissionCommandHandler(
        IAccessPermissionRepository permissionRepository,
        RoleManager<IdentityRole> roleManager)
    {
        _permissionRepository = permissionRepository;
        _roleManager = roleManager;
    }

    public async Task<Result> Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (permission is null)
        {
            return Result.Failure("مجوز مورد نظر یافت نشد.");
        }

        if (permission.IsCore)
        {
            return Result.Failure("این مجوز به عنوان Core ثبت شده و امکان حذف آن وجود ندارد.");
        }

        var descendantPrefix = permission.Key + ".";
        var descendants = await _permissionRepository.GetByKeyPrefixAsync(descendantPrefix, cancellationToken);

        var keysToRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            permission.Key
        };

        foreach (var descendant in descendants)
        {
            keysToRemove.Add(descendant.Key);
        }

        var roles = await _roleManager.Roles
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var permissionClaims = claims
                .Where(claim => string.Equals(claim.Type, PermissionCatalog.ClaimType, StringComparison.OrdinalIgnoreCase))
                .Where(claim => keysToRemove.Contains(claim.Value))
                .ToArray();

            foreach (var claim in permissionClaims)
            {
                var result = await _roleManager.RemoveClaimAsync(role, claim);
                if (!result.Succeeded)
                {
                    return Result.Failure(string.Join("؛ ", result.Errors.Select(error => error.Description)));
                }
            }
        }

        _permissionRepository.RemoveRange(descendants);
        _permissionRepository.Remove(permission);
        await _permissionRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
