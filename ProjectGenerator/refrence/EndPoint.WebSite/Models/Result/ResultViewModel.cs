using System.Collections.Generic;

namespace EndPoint.WebSite.Models.Result;

public sealed class ResultViewModel
{
    public int RunId { get; init; }

    public IReadOnlyList<ScoreItemVm> PvqScores { get; init; } = Array.Empty<ScoreItemVm>();
    public IReadOnlyList<ScoreItemVm> CliftonScores { get; init; } = Array.Empty<ScoreItemVm>();
    public IReadOnlyList<JobGroupScoreVm> JobGroups { get; init; } = Array.Empty<JobGroupScoreVm>();
    public IReadOnlyList<JobSkillPlanVm> TopPlans { get; init; } = Array.Empty<JobSkillPlanVm>();
}

public sealed class JobSkillPlanVm
{
    public string JobGroup { get; init; } = string.Empty;
    public IReadOnlyList<string> SG1 { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SG2 { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SG3 { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SG4 { get; init; } = Array.Empty<string>();
}

public sealed class ScoreItemVm
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public double Score { get; init; }
}

public sealed class JobGroupScoreVm
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public double Score { get; init; }
}
