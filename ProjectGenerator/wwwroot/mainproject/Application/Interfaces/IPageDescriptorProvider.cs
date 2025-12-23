using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MobiRooz.Application.DTOs;

namespace MobiRooz.Application.Interfaces;

public interface IPageDescriptorProvider
{
    Task<IReadOnlyCollection<PageDescriptorDto>> GetAdminPageDescriptorsAsync(CancellationToken cancellationToken);
}
