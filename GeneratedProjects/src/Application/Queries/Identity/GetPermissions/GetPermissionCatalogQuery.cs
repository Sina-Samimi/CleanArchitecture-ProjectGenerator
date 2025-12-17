using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.DTOs;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Queries.Identity.GetPermissions;

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
