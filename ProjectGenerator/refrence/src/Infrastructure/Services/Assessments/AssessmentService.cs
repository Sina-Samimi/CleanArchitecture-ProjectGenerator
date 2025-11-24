using Arsis.Application.Assessments;
using Arsis.Application.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arsis.Infrastructure.Services.Assessments;

public sealed class AssessmentService : IAssessmentService
{
    private readonly CorrelationMatrixProvider _matrixProvider;
    private readonly IScoringStrategyResolver _strategyResolver;
    private readonly ScoringOptions _options;
    private readonly ILogger<AssessmentService> _logger;

    public AssessmentService(
        CorrelationMatrixProvider matrixProvider,
        IScoringStrategyResolver strategyResolver,
        IOptions<ScoringOptions> options,
        ILogger<AssessmentService> logger)
    {
        _matrixProvider = matrixProvider;
        _strategyResolver = strategyResolver;
        _options = options.Value;
        _logger = logger;
    }

    public Task<AssessmentResponse> EvaluateAsync(AssessmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var (response, _) = EvaluateInternal(request, null, includeDebug: false);
        _logger.LogInformation("Assessment evaluated for user {UserId} with inventory {InventoryId}", request.UserId, request.InventoryId);
        return Task.FromResult(response);
    }

    public Task<AssessmentDebugResponse> EvaluateWithDebugAsync(AssessmentRequest request, string? jobGroup, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var (_, debug) = EvaluateInternal(request, jobGroup, includeDebug: true);
        if (debug is null)
        {
            throw new InvalidOperationException("Debug information was not generated.");
        }

        _logger.LogInformation("Assessment evaluated with debug for user {UserId} (jobGroup={JobGroup})", request.UserId, jobGroup ?? "auto");
        return Task.FromResult(debug);
    }

    public Task<MatricesOverview> GetMatricesOverviewAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var matrices = _matrixProvider.GetMatrices();
        var metadata = _matrixProvider.GetMetadata();
        return Task.FromResult(new MatricesOverview(metadata, matrices.LoadedAt));
    }

    private (AssessmentResponse Response, AssessmentDebugResponse? Debug) EvaluateInternal(AssessmentRequest request, string? debugJobGroup, bool includeDebug)
    {
        var matrices = _matrixProvider.GetMatrices();
        var strategy = _strategyResolver.Resolve(_options.Strategy);

        var cliftonScores = Normalize(request.Cilifton, matrices.TalentIds, strategy, _options.CliftonMaxLikert);
        var pvqScores = Normalize(request.Pvq, matrices.ValueIds, strategy, _options.PvqMaxLikert, _options.NormalizePvqOverMax);

        var cliftonVector = Vector<double>.Build.Dense(matrices.TalentIds.Count, i => cliftonScores[matrices.TalentIds[i]]);
        var pvqVector = Vector<double>.Build.Dense(matrices.ValueIds.Count, i => pvqScores[matrices.ValueIds[i]]);

        var gFromClifton = cliftonVector * matrices.TalentJob;
        var gFromPvq = pvqVector * matrices.ValueJob;

        var gFinal = gFromClifton * _options.Alpha + gFromPvq * _options.Beta;

        var jobScoresDict = BuildDictionary(matrices.JobGroupIds, gFinal);
        var orderedJobs = jobScoresDict
            .OrderByDescending(pair => pair.Value)
            .Take(Math.Max(1, _options.TopJobCount))
            .ToList();

        var sFromTalent = cliftonVector * matrices.TalentSkill;
        var skillPlans = new List<SkillPlan>();

        foreach (var (jobId, score) in orderedJobs)
        {
            var jobColumnIndex = FindIndex(matrices.JobGroupIds, jobId);
            if (jobColumnIndex < 0)
            {
                continue;
            }

            var jobSkillProfile = matrices.JobSkill.Row(jobColumnIndex);
            var sJob = sFromTalent + jobSkillProfile;
            var plan = BuildSkillPlan(jobId, sJob, matrices.SkillIds, matrices.SkillLevelMap, _options.TopSkillsPerLevel);
            skillPlans.Add(plan);
        }

        var normalizedScores = new NormalizedScores(cliftonScores, pvqScores);
        var response = new AssessmentResponse(normalizedScores, jobScoresDict, skillPlans);

        if (!includeDebug)
        {
            return (response, null);
        }

        var debugJob = debugJobGroup ?? orderedJobs.FirstOrDefault().Key ?? matrices.JobGroupIds.First();
        var debugJobIndex = FindIndex(matrices.JobGroupIds, debugJob);
        if (debugJobIndex < 0)
        {
            debugJobIndex = 0;
            debugJob = matrices.JobGroupIds[0];
        }

        var sJobDebugVector = sFromTalent + matrices.JobSkill.Row(debugJobIndex);

        var debug = new AssessmentDebugResponse(
            normalizedScores,
            BuildDictionary(matrices.JobGroupIds, gFromClifton),
            BuildDictionary(matrices.JobGroupIds, gFromPvq),
            jobScoresDict,
            BuildDictionary(matrices.SkillIds, sFromTalent),
            BuildDictionary(matrices.SkillIds, sJobDebugVector),
            skillPlans);

        return (response, debug);
    }

    private IReadOnlyDictionary<string, double> Normalize(
        IDictionary<string, IDictionary<string, int>> source,
        IReadOnlyList<string> expectedIds,
        IScoringStrategy strategy,
        int maxLikert,
        bool divideByMax = true)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in expectedIds)
        {
            if (!source.TryGetValue(id, out var responses) || responses.Count == 0)
            {
                result[id] = 0d;
                continue;
            }

            var values = responses.Values;
            var score = strategy.Score(values, maxLikert);
            result[id] = divideByMax ? score : Math.Round(values.Average(), 6, MidpointRounding.AwayFromZero);
        }

        return result;
    }

    private static IReadOnlyDictionary<string, double> BuildDictionary(IReadOnlyList<string> keys, Vector<double> vector)
    {
        var dictionary = new Dictionary<string, double>(keys.Count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < keys.Count; i++)
        {
            dictionary[keys[i]] = Math.Round(vector[i], 6, MidpointRounding.AwayFromZero);
        }

        return dictionary;
    }

    private static SkillPlan BuildSkillPlan(
        string jobGroup,
        Vector<double> scores,
        IReadOnlyList<string> skillIds,
        IReadOnlyDictionary<string, int> skillLevelMap,
        int topPerLevel)
    {
        var buckets = new Dictionary<int, List<(string SkillId, double Score)>>();

        for (var i = 0; i < skillIds.Count; i++)
        {
            var skillId = skillIds[i];
            if (!skillLevelMap.TryGetValue(skillId, out var level))
            {
                continue;
            }

            if (!buckets.TryGetValue(level, out var list))
            {
                list = new List<(string SkillId, double Score)>();
                buckets[level] = list;
            }

            list.Add((skillId, scores[i]));
        }

        static IReadOnlyList<string> TopN(IDictionary<int, List<(string SkillId, double Score)>> bucket, int level, int count)
        {
            if (!bucket.TryGetValue(level, out var list) || list.Count == 0)
            {
                return Array.Empty<string>();
            }

            return list
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.SkillId, StringComparer.OrdinalIgnoreCase)
                .Take(count)
                .Select(item => item.SkillId)
                .ToList();
        }

        var awareness = TopN(buckets, 1, topPerLevel);
        var selfBuilding = TopN(buckets, 2, topPerLevel);
        var selfDevelopment = TopN(buckets, 3, topPerLevel);
        var selfActualization = TopN(buckets, 4, topPerLevel);

        return new SkillPlan(jobGroup, awareness, selfBuilding, selfDevelopment, selfActualization);
    }

    private static int FindIndex(IReadOnlyList<string> source, string value)
    {
        for (var i = 0; i < source.Count; i++)
        {
            if (string.Equals(source[i], value, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
