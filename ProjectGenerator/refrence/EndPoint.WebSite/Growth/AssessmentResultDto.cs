using System.Collections.Generic;

namespace EndPoint.WebSite.Growth;

public sealed record JobScoreDto(string JobCode, double Score);

public sealed record SkillPlanDto(
    string JobCode,
    IReadOnlyList<string> SelfAwareness,
    IReadOnlyList<string> SelfBuilding,
    IReadOnlyList<string> SelfDevelopment,
    IReadOnlyList<string> SelfActualization);

public sealed record AssessmentResultDto(
    IReadOnlyDictionary<string, double> CliftonScores,
    IReadOnlyDictionary<string, double> PvqScores,
    IReadOnlyList<JobScoreDto> JobScores,
    IReadOnlyList<SkillPlanDto> SkillPlans);
