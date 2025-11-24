namespace EndPoint.WebSite.Growth;

public sealed class CorrelationMatrices
{
    public required IReadOnlyList<string> TalentIds { get; init; }

    public required IReadOnlyList<string> ValueIds { get; init; }

    public required IReadOnlyList<string> JobIds { get; init; }

    public required IReadOnlyList<string> SkillIds { get; init; }

    public required double[,] TalentJob { get; init; }

    public required double[,] ValueJob { get; init; }

    public required double[,] TalentSkill { get; init; }

    public required double[,] JobSkill { get; init; }

    public required IReadOnlyDictionary<string, int> SkillLevelMap { get; init; }
}
