using Arsis.Application.Interfaces;
using Arsis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class TalentRepository : ITalentRepository
{
    private readonly AppDbContext _dbContext;

    public TalentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Talent>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.Talents.AsNoTracking().ToListAsync(cancellationToken);
}
