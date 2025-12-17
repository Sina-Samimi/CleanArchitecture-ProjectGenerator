using System.Collections.Generic;
using System.Threading;
using LogTableRenameTest.Application.DTOs;

namespace LogTableRenameTest.Application.Interfaces;

public interface IPermissionDefinitionService
{
    Task<IReadOnlyCollection<PermissionGroupDto>> GetPermissionGroupsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, PermissionDefinitionDto>> GetDefinitionsLookupAsync(CancellationToken cancellationToken);

    Task<HashSet<string>> GetAllKeysAsync(CancellationToken cancellationToken);
}
