using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.DTOs;

namespace LogTableRenameTest.Application.Interfaces;

public interface IPageDescriptorProvider
{
    Task<IReadOnlyCollection<PageDescriptorDto>> GetAdminPageDescriptorsAsync(CancellationToken cancellationToken);
}
