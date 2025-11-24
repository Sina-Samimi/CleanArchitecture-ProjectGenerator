using System.Threading;
using System.Threading.Tasks;
using Arsis.Domain.Entities.Assessments;
using Arsis.Domain.Enums;

namespace Arsis.Application.Interfaces;

public interface IAssessmentQuestionRepository
{
    Task<AssessmentQuestion?> GetByTestTypeAndIndexAsync(
        AssessmentTestType testType,
        int index,
        CancellationToken cancellationToken = default);

    Task<List<AssessmentQuestion>> GetByTestTypeAsync(
        AssessmentTestType testType,
        CancellationToken cancellationToken = default);
}
