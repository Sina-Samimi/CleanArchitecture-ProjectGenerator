using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs;

namespace Arsis.Application.Interfaces;

public interface IPageDescriptorProvider
{
    Task<IReadOnlyCollection<PageDescriptorDto>> GetAdminPageDescriptorsAsync(CancellationToken cancellationToken);
}
