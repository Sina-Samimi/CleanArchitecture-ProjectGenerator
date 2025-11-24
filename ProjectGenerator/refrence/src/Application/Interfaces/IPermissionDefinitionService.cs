using System.Collections.Generic;
using System.Threading;
using Arsis.Application.DTOs;

namespace Arsis.Application.Interfaces;

public interface IPermissionDefinitionService
{
    Task<IReadOnlyCollection<PermissionGroupDto>> GetPermissionGroupsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, PermissionDefinitionDto>> GetDefinitionsLookupAsync(CancellationToken cancellationToken);

    Task<HashSet<string>> GetAllKeysAsync(CancellationToken cancellationToken);
}
