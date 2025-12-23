using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Domain.Entities;

namespace MobiRooz.Application.Interfaces;

public interface IPageAccessPolicyRepository
{
    Task<IReadOnlyCollection<PageAccessPolicy>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PageAccessPolicy>> GetByPageAsync(
        string area,
        string controller,
        string action,
        CancellationToken cancellationToken);

    Task ReplacePoliciesAsync(
        string area,
        string controller,
        string action,
        IReadOnlyCollection<string> permissionKeys,
        CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
