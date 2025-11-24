using System;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.Authorization;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Identity.GetPermissions;

public sealed record GetPermissionByIdQuery(Guid Id) : IQuery<PermissionDetailsDto>;

public sealed class GetPermissionByIdQueryHandler : IQueryHandler<GetPermissionByIdQuery, PermissionDetailsDto>
{
    private readonly IAccessPermissionRepository _permissionRepository;

    public GetPermissionByIdQueryHandler(IAccessPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<Result<PermissionDetailsDto>> Handle(GetPermissionByIdQuery request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (permission is null)
        {
            return Result<PermissionDetailsDto>.Failure("مجوز مورد نظر یافت نشد.");
        }

        var groupKey = PermissionGroupUtility.NormalizeGroupKey(permission.GroupKey);
        var groupDisplayName = string.IsNullOrWhiteSpace(permission.GroupDisplayName)
            ? PermissionGroupUtility.ResolveGroupDisplayName(groupKey, permission.GroupKey)
            : permission.GroupDisplayName;

        var dto = new PermissionDetailsDto(
            permission.Id,
            permission.Key,
            permission.DisplayName,
            permission.Description,
            permission.IsCore,
            permission.CreatedAt,
            permission.UpdatedAt,
            groupKey,
            groupDisplayName);

        return Result<PermissionDetailsDto>.Success(dto);
    }
}
