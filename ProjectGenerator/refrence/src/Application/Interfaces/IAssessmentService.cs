using Arsis.Application.Assessments;

namespace Arsis.Application.Interfaces;

public interface IAssessmentService
{
    Task<AssessmentResponse> EvaluateAsync(AssessmentRequest request, CancellationToken cancellationToken);

    Task<AssessmentDebugResponse> EvaluateWithDebugAsync(AssessmentRequest request, string? jobGroup, CancellationToken cancellationToken);

    Task<MatricesOverview> GetMatricesOverviewAsync(CancellationToken cancellationToken);
}
