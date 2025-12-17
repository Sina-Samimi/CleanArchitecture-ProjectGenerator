using System.Collections.Generic;
using System.Threading;
using TestAttarClone.Application.DTOs;

namespace TestAttarClone.Application.Interfaces;

public interface IPermissionDefinitionService
{
    Task<IReadOnlyCollection<PermissionGroupDto>> GetPermissionGroupsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, PermissionDefinitionDto>> GetDefinitionsLookupAsync(CancellationToken cancellationToken);

    Task<HashSet<string>> GetAllKeysAsync(CancellationToken cancellationToken);
}
