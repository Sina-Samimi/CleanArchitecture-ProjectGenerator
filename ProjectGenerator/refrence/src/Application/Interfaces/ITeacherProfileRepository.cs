using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Domain.Entities.Teachers;

namespace Arsis.Application.Interfaces;

public interface ITeacherProfileRepository
{
    Task<IReadOnlyCollection<TeacherProfile>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TeacherProfile>> GetActiveAsync(CancellationToken cancellationToken);

    Task<TeacherProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<TeacherProfile?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<TeacherProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<bool> ExistsByUserIdAsync(string userId, Guid? excludeId, CancellationToken cancellationToken);

    Task AddAsync(TeacherProfile teacher, CancellationToken cancellationToken);

    Task UpdateAsync(TeacherProfile teacher, CancellationToken cancellationToken);
}
