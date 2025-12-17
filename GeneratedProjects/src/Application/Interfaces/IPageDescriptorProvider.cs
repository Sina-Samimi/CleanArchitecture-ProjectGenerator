using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.DTOs;

namespace TestAttarClone.Application.Interfaces;

public interface IPageDescriptorProvider
{
    Task<IReadOnlyCollection<PageDescriptorDto>> GetAdminPageDescriptorsAsync(CancellationToken cancellationToken);
}
