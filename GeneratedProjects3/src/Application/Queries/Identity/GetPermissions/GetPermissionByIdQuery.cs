using System;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.Authorization;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Identity.GetPermissions;

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
