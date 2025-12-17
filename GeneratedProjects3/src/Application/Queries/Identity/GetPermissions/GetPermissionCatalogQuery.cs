using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Identity.GetPermissions;

public sealed record GetPermissionCatalogQuery : IQuery<PermissionCatalogDto>;

public sealed class GetPermissionCatalogQueryHandler : IQueryHandler<GetPermissionCatalogQuery, PermissionCatalogDto>
{
    private readonly IPermissionDefinitionService _permissionDefinitionService;

    public GetPermissionCatalogQueryHandler(IPermissionDefinitionService permissionDefinitionService)
    {
        _permissionDefinitionService = permissionDefinitionService;
    }

    public async Task<Result<PermissionCatalogDto>> Handle(GetPermissionCatalogQuery request, CancellationToken cancellationToken)
    {
        var groups = await _permissionDefinitionService.GetPermissionGroupsAsync(cancellationToken);
        var lookup = await _permissionDefinitionService.GetDefinitionsLookupAsync(cancellationToken);

        return Result<PermissionCatalogDto>.Success(new PermissionCatalogDto(groups, lookup));
    }
}
