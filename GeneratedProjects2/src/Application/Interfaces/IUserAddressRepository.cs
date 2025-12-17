using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Domain.Entities;

namespace LogsDtoCloneTest.Application.Interfaces;

public interface IUserAddressRepository
{
    Task<UserAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<UserAddress?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserAddress>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<UserAddress?> GetDefaultByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task AddAsync(UserAddress address, CancellationToken cancellationToken);

    Task UpdateAsync(UserAddress address, CancellationToken cancellationToken);

    Task RemoveAsync(UserAddress address, CancellationToken cancellationToken);
}
