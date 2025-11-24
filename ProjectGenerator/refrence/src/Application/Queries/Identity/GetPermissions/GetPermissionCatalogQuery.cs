using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Identity.GetPermissions;

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
