using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Domain.Entities.Contacts;

namespace LogTableRenameTest.Application.Interfaces;

public interface IContactMessageRepository
{
    Task<ContactMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(ContactMessage message, CancellationToken cancellationToken);

    Task UpdateAsync(ContactMessage message, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ContactMessage>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ContactMessage>> GetUnreadAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<int> GetCountAsync(CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken);
}

