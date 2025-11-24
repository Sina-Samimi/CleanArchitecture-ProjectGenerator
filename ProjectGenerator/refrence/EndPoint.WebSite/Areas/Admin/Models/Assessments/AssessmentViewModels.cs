using Arsis.Application.Assessments;

namespace EndPoint.WebSite.Areas.Admin.Models.Assessments;

public sealed class AssessmentMatrixViewModel
{
    public required string Name { get; init; }

    public int Rows { get; init; }

    public int Columns { get; init; }

    public DateTimeOffset LoadedAt { get; init; }

    public required string SourcePath { get; init; }
}

public sealed class AssessmentCalculationViewModel
{
    public AssessmentCalculationViewModel()
    {
        RequestJson = string.Empty;
    }

    public string RequestJson { get; set; }

    public AssessmentResponse? Result { get; set; }

    public AssessmentDebugResponse? DebugResult { get; set; }

    public string? Error { get; set; }

    public IReadOnlyCollection<AssessmentMatrixViewModel> Matrices { get; set; } = Array.Empty<AssessmentMatrixViewModel>();
}
