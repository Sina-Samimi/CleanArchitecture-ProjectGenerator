using Arsis.Application.Interfaces;
using Arsis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class QuestionRepository : IQuestionRepository
{
    private readonly AppDbContext _dbContext;

    public QuestionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Question>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.Questions.AsNoTracking().ToListAsync(cancellationToken);
}
