namespace Arsis.Application.Assessments;

public sealed class MatricesOptions
{
    public string TalentJob { get; set; } = string.Empty;

    public string ValueJob { get; set; } = string.Empty;

    public string TalentSkill { get; set; } = string.Empty;

    public string JobSkill { get; set; } = string.Empty;

    public string SkillLevels { get; set; } = string.Empty;
}

public sealed class ScoringOptions
{
    public int CliftonMaxLikert { get; set; } = 2;

    public int PvqMaxLikert { get; set; } = 6;

    public double Alpha { get; set; } = 1.0d;

    public double Beta { get; set; } = 1.0d;

    public string Strategy { get; set; } = "MeanOverMax";

    public bool NormalizePvqOverMax { get; set; } = true;

    public int TopJobCount { get; set; } = 3;

    public int TopSkillsPerLevel { get; set; } = 5;
}
