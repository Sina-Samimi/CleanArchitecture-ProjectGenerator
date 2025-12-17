using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.DTOs;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface IPageDescriptorProvider
{
    Task<IReadOnlyCollection<PageDescriptorDto>> GetAdminPageDescriptorsAsync(CancellationToken cancellationToken);
}
