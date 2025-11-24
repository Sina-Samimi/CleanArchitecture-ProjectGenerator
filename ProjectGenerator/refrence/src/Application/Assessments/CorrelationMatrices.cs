using MathNet.Numerics.LinearAlgebra;

namespace Arsis.Application.Assessments;

public sealed class CorrelationMatrices
{
    public CorrelationMatrices(
        Matrix<double> talentJob,
        Matrix<double> valueJob,
        Matrix<double> talentSkill,
        Matrix<double> jobSkill,
        IReadOnlyList<string> talentIds,
        IReadOnlyList<string> valueIds,
        IReadOnlyList<string> jobGroupIds,
        IReadOnlyList<string> skillIds,
        IReadOnlyDictionary<string, int> skillLevelMap,
        DateTimeOffset loadedAt)
    {
        TalentJob = talentJob ?? throw new ArgumentNullException(nameof(talentJob));
        ValueJob = valueJob ?? throw new ArgumentNullException(nameof(valueJob));
        TalentSkill = talentSkill ?? throw new ArgumentNullException(nameof(talentSkill));
        JobSkill = jobSkill ?? throw new ArgumentNullException(nameof(jobSkill));
        TalentIds = talentIds ?? throw new ArgumentNullException(nameof(talentIds));
        ValueIds = valueIds ?? throw new ArgumentNullException(nameof(valueIds));
        JobGroupIds = jobGroupIds ?? throw new ArgumentNullException(nameof(jobGroupIds));
        SkillIds = skillIds ?? throw new ArgumentNullException(nameof(skillIds));
        SkillLevelMap = skillLevelMap ?? throw new ArgumentNullException(nameof(skillLevelMap));
        LoadedAt = loadedAt;
    }

    public Matrix<double> TalentJob { get; }

    public Matrix<double> ValueJob { get; }

    public Matrix<double> TalentSkill { get; }

    public Matrix<double> JobSkill { get; }

    public IReadOnlyList<string> TalentIds { get; }

    public IReadOnlyList<string> ValueIds { get; }

    public IReadOnlyList<string> JobGroupIds { get; }

    public IReadOnlyList<string> SkillIds { get; }

    public IReadOnlyDictionary<string, int> SkillLevelMap { get; }

    public DateTimeOffset LoadedAt { get; }
}
