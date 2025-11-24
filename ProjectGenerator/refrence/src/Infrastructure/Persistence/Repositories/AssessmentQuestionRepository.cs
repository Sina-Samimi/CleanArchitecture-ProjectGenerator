using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Assessments;
using Arsis.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Persistence.Repositories;

public sealed class AssessmentQuestionRepository : IAssessmentQuestionRepository
{
    private readonly AppDbContext _dbContext;

    public AssessmentQuestionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AssessmentQuestion?> GetByTestTypeAndIndexAsync(
        AssessmentTestType testType,
        int index,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.AssessmentQuestions
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.TestType == testType && q.Index == index, cancellationToken);
    }

    public Task<List<AssessmentQuestion>> GetByTestTypeAsync(
        AssessmentTestType testType,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.AssessmentQuestions
            .AsNoTracking()
            .Where(q => q.TestType == testType)
            .OrderBy(q => q.Index)
            .ToListAsync(cancellationToken);
    }
}
