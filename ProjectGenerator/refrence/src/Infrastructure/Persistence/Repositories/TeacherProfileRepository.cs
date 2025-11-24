using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Teachers;
using Arsis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class TeacherProfileRepository : ITeacherProfileRepository
{
    private readonly AppDbContext _dbContext;

    public TeacherProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<TeacherProfile>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.TeacherProfiles
            .AsNoTracking()
            .Where(teacher => !teacher.IsDeleted)
            .OrderByDescending(teacher => teacher.UpdateDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<TeacherProfile>> GetActiveAsync(CancellationToken cancellationToken)
        => await _dbContext.TeacherProfiles
            .AsNoTracking()
            .Where(teacher => !teacher.IsDeleted && teacher.IsActive)
            .OrderBy(teacher => teacher.DisplayName)
            .ToListAsync(cancellationToken);

    public async Task<TeacherProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.TeacherProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(teacher => teacher.Id == id && !teacher.IsDeleted, cancellationToken);

    public async Task<TeacherProfile?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.TeacherProfiles
            .FirstOrDefaultAsync(teacher => teacher.Id == id && !teacher.IsDeleted, cancellationToken);

    public async Task<TeacherProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var normalized = userId.Trim();

        return await _dbContext.TeacherProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                teacher => !teacher.IsDeleted && teacher.UserId == normalized,
                cancellationToken);
    }

    public async Task<bool> ExistsByUserIdAsync(string userId, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var normalized = userId.Trim();

        var query = _dbContext.TeacherProfiles
            .AsNoTracking()
            .Where(teacher => !teacher.IsDeleted && teacher.UserId == normalized);

        if (excludeId.HasValue)
        {
            var excluded = excludeId.Value;
            query = query.Where(teacher => teacher.Id != excluded);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(TeacherProfile teacher, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teacher);

        await _dbContext.TeacherProfiles.AddAsync(teacher, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TeacherProfile teacher, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teacher);

        _dbContext.TeacherProfiles.Update(teacher);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
