using Arsis.Application.Assessments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arsis.Infrastructure.Services.Assessments;

public sealed class CorrelationMatrixProvider
{
    private readonly MatricesOptions _options;
    private readonly MatrixLoader _loader;
    private readonly ILogger<CorrelationMatrixProvider> _logger;
    private readonly object _sync = new();
    private CorrelationMatrices? _matrices;
    private IReadOnlyCollection<MatrixMetadata>? _metadata;

    public CorrelationMatrixProvider(
        IOptions<MatricesOptions> options,
        MatrixLoader loader,
        ILogger<CorrelationMatrixProvider> logger,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(environment);

        _options = options.Value;
        _loader = loader;
        _logger = logger;

        ContentRootPath = environment.ContentRootPath;
    }

    private string ContentRootPath { get; }

    public CorrelationMatrices GetMatrices()
    {
        if (_matrices is not null)
        {
            return _matrices;
        }

        lock (_sync)
        {
            if (_matrices is not null)
            {
                return _matrices;
            }

            _matrices = Load();
            return _matrices;
        }
    }

    public IReadOnlyCollection<MatrixMetadata> GetMetadata()
    {
        if (_metadata is not null)
        {
            return _metadata;
        }

        lock (_sync)
        {
            if (_metadata is not null)
            {
                return _metadata;
            }

            if (_matrices is null)
            {
                _matrices = Load();
            }

            return _metadata ?? Array.Empty<MatrixMetadata>();
        }
    }

    private CorrelationMatrices Load()
    {
        _logger.LogInformation("Loading correlation matrices from configured sources.");

        var talentJobPath = ResolvePath(_options.TalentJob);
        var valueJobPath = ResolvePath(_options.ValueJob);
        var talentSkillPath = ResolvePath(_options.TalentSkill);
        var jobSkillPath = ResolvePath(_options.JobSkill);
        var skillLevelsPath = ResolvePath(_options.SkillLevels);

        var (talentJob, talentIds, jobGroupIds) = _loader.LoadMatrix(talentJobPath, "TalentJob");
        var (valueJob, valueIds, jobGroupIdsFromValue) = _loader.LoadMatrix(valueJobPath, "ValueJob");

        ValidateAlignment(jobGroupIds, jobGroupIdsFromValue, "JobGroup columns between TalentJob and ValueJob");

        var (talentSkill, talentIdsFromSkill, skillIds) = _loader.LoadMatrix(talentSkillPath, "TalentSkill");
        ValidateAlignment(talentIds, talentIdsFromSkill, "Talent rows between TalentJob and TalentSkill");

        var (jobSkill, jobGroupIdsFromSkill, skillIdsFromJobSkill) = _loader.LoadMatrix(jobSkillPath, "JobSkill");
        ValidateAlignment(jobGroupIds, jobGroupIdsFromSkill, "JobGroup rows between TalentJob and JobSkill");
        ValidateAlignment(skillIds, skillIdsFromJobSkill, "Skill columns between TalentSkill and JobSkill");

        var skillLevelMap = _loader.LoadSkillLevels(skillLevelsPath);
        ValidateSkillLevels(skillIds, skillLevelMap);

        var loadedAt = DateTimeOffset.UtcNow;

        var metadata = new List<MatrixMetadata>
        {
            BuildMetadata("TalentJob", talentJobPath, talentJob.RowCount, talentJob.ColumnCount, loadedAt),
            BuildMetadata("ValueJob", valueJobPath, valueJob.RowCount, valueJob.ColumnCount, loadedAt),
            BuildMetadata("TalentSkill", talentSkillPath, talentSkill.RowCount, talentSkill.ColumnCount, loadedAt),
            BuildMetadata("JobSkill", jobSkillPath, jobSkill.RowCount, jobSkill.ColumnCount, loadedAt),
            new MatrixMetadata("SkillLevels", skillLevelMap.Count, 2, loadedAt, skillLevelsPath)
        };

        _metadata = metadata;

        return new CorrelationMatrices(
            talentJob,
            valueJob,
            talentSkill,
            jobSkill,
            talentIds,
            valueIds,
            jobGroupIds,
            skillIds,
            skillLevelMap,
            loadedAt);
    }

    private static void ValidateAlignment(IReadOnlyList<string> baseline, IReadOnlyList<string> candidate, string description)
    {
        if (baseline.Count != candidate.Count)
        {
            throw new InvalidOperationException($"Alignment mismatch for {description}: {baseline.Count} vs {candidate.Count}");
        }

        for (var i = 0; i < baseline.Count; i++)
        {
            if (!string.Equals(baseline[i], candidate[i], StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Alignment mismatch for {description} at index {i}: '{baseline[i]}' vs '{candidate[i]}'");
            }
        }
    }

    private static void ValidateSkillLevels(IEnumerable<string> skills, IReadOnlyDictionary<string, int> map)
    {
        var missing = skills.Where(skill => !map.ContainsKey(skill)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"Skill levels are missing for {missing.Count} skills: {string.Join(", ", missing.Take(5))}{(missing.Count > 5 ? "..." : string.Empty)}");
        }
    }

    private MatrixMetadata BuildMetadata(string name, string path, int rows, int columns, DateTimeOffset loadedAt)
    {
        return new MatrixMetadata(name, rows, columns, loadedAt, path);
    }

    private string ResolvePath(string relativeOrAbsolute)
    {
        if (Path.IsPathRooted(relativeOrAbsolute))
        {
            return relativeOrAbsolute;
        }

        if (string.IsNullOrWhiteSpace(relativeOrAbsolute))
        {
            throw new ArgumentException("Matrix path cannot be empty.");
        }

        return Path.GetFullPath(Path.Combine(ContentRootPath, relativeOrAbsolute));
    }
}
