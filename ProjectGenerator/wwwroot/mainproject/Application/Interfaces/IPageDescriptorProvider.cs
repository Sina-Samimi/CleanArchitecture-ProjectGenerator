using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs;

namespace Attar.Application.Interfaces;

public interface IPageDescriptorProvider
{
    Task<IReadOnlyCollection<PageDescriptorDto>> GetAdminPageDescriptorsAsync(CancellationToken cancellationToken);
}
