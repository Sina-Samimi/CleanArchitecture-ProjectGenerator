using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using Arsis.Application.Assessments;
using Arsis.Domain.Entities.Assessments;
using Arsis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EndPoint.WebSite.Growth;

public sealed class AssessmentService
{
    private readonly AppDbContext _dbContext;
    private readonly CorrelationMatrices _matrices;
    private readonly ScoringOptions _options;
    private readonly int _pvqMaxLikert;
    private readonly int _topJobCount;
    private readonly int _topSkillsPerLevel;
    private readonly double _alpha;
    private readonly double _beta;

    public AssessmentService(
        AppDbContext dbContext,
        CorrelationMatrices matrices,
        IOptionsMonitor<ScoringOptions> options)
    {
        _dbContext = dbContext;
        _matrices = matrices;
        _options = options.CurrentValue;
        _pvqMaxLikert = _options.PvqMaxLikert;
        _topJobCount = Math.Max(1, _options.TopJobCount);
        _topSkillsPerLevel = Math.Max(1, _options.TopSkillsPerLevel);
        _alpha = _options.Alpha;
        _beta = _options.Beta;
    }

    public async Task<AssessmentResultDto> EvaluateAsync(int runId, CancellationToken cancellationToken)
    {
        var run = await _dbContext.AssessmentRuns
            .Include(r => r.Responses)
                .ThenInclude(r => r.Question)
            .SingleAsync(r => r.Id == runId, cancellationToken);

        if (run.CompletedAt is null)
        {
            run.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var cliftonScores = ComputeCliftonVector(run);
        var pvqScores = ComputePvqVector(run);

        var gFromClifton = Multiply(cliftonScores.ValuesArray, _matrices.TalentJob);
        var gFromPvq = Multiply(pvqScores.ValuesArray, _matrices.ValueJob);
        var gFinal = Add(Scale(gFromClifton, _alpha), Scale(gFromPvq, _beta));

        var sFromTalent = Multiply(cliftonScores.ValuesArray, _matrices.TalentSkill);

        var jobScores = _matrices.JobIds
            .Select((job, index) => new JobScoreDto(job, Math.Round(gFinal[index], 6)))
            .OrderByDescending(j => j.Score)
            .ToList();

        var topJobs = jobScores.Take(_topJobCount).ToList();
        var skillPlans = new List<SkillPlanDto>();

        foreach (var job in topJobs)
        {
            var jobIndex = FindIndex(_matrices.JobIds, job.JobCode);
            if (jobIndex < 0)
            {
                continue;
            }

            var jobSkillVector = GetRow(_matrices.JobSkill, jobIndex);
            var combinedSkills = Add(sFromTalent, jobSkillVector);
            skillPlans.Add(BuildPlan(job.JobCode, combinedSkills));
        }

        return new AssessmentResultDto(
            cliftonScores.Dictionary,
            pvqScores.Dictionary,
            jobScores,
            skillPlans);
    }

    private CliftonResult ComputeCliftonVector(AssessmentRun run)
    {
        var totals = new ConcurrentDictionary<string, (int chosen, int total)>(StringComparer.OrdinalIgnoreCase);

        void IncrementTotal(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            totals.AddOrUpdate(code.Trim().ToUpperInvariant(), (0, 1), (_, value) => (value.chosen, value.total + 1));
        }

        void IncrementChosen(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            totals.AddOrUpdate(code.Trim().ToUpperInvariant(), (1, 1), (_, value) => (value.chosen + 1, value.total));
        }

        var answeredCliftonQuestions = run.Responses
            .Where(r => r.Question.TestType == AssessmentTestType.Clifton)
            .Select(r => r.Question)
            .DistinctBy(q => q.Id)
            .ToList();

        foreach (var question in answeredCliftonQuestions)
        {
            IncrementTotal(question.TalentCodeA);
            IncrementTotal(question.TalentCodeB);
        }

        foreach (var response in run.Responses.Where(r => r.Question.TestType == AssessmentTestType.Clifton))
        {
            if (string.Equals(response.Answer, "A", StringComparison.OrdinalIgnoreCase))
            {
                IncrementChosen(response.Question.TalentCodeA);
            }
            else if (string.Equals(response.Answer, "B", StringComparison.OrdinalIgnoreCase))
            {
                IncrementChosen(response.Question.TalentCodeB);
            }
        }

        var vector = new double[_matrices.TalentIds.Count];
        var dictionary = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < _matrices.TalentIds.Count; i++)
        {
            var code = _matrices.TalentIds[i];
            var (chosen, total) = totals.GetValueOrDefault(code, (0, 0));
            var score = total == 0 ? 0d : Math.Round((double)chosen / total, 6);
            vector[i] = score;
            dictionary[code] = score;
        }

        return new CliftonResult(vector, dictionary);
    }

    private PvqResult ComputePvqVector(AssessmentRun run)
    {
        var responses = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);

        foreach (var response in run.Responses)
        {
            if (response.Question.TestType != AssessmentTestType.Pvq)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(response.Answer))
            {
                continue;
            }

            var code = response.Question.PvqCode;
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            var parsed = ParseLikert(response.Answer);
            if (!parsed.HasValue)
            {
                continue;
            }

            var min = response.Question.LikertMin ?? 1;
            var max = response.Question.LikertMax ?? _pvqMaxLikert;
            if (max <= min)
            {
                max = Math.Max(min + 1, _pvqMaxLikert);
            }

            var value = Math.Clamp(parsed.Value, min, max);
            if (response.Question.IsReverse == true)
            {
                value = min + (max - value);
            }

            var normalized = (value - min) / (double)(max - min);
            var normalizedCode = code.Trim().ToUpperInvariant();

            if (!responses.TryGetValue(normalizedCode, out var bucket))
            {
                bucket = new List<double>();
                responses[normalizedCode] = bucket;
            }

            bucket.Add(normalized);
        }

        var vector = new double[_matrices.ValueIds.Count];
        var dictionary = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < _matrices.ValueIds.Count; i++)
        {
            var code = _matrices.ValueIds[i];
            if (responses.TryGetValue(code, out var values) && values.Count > 0)
            {
                var average = Math.Round(values.Average(), 6);
                vector[i] = average;
                dictionary[code] = average;
            }
            else
            {
                vector[i] = 0d;
                dictionary[code] = 0d;
            }
        }

        return new PvqResult(vector, dictionary);
    }

    private SkillPlanDto BuildPlan(string jobCode, double[] skillVector)
    {
        var skillScores = _matrices.SkillIds
            .Select((skill, index) => new
            {
                Skill = skill,
                Level = _matrices.SkillLevelMap.TryGetValue(skill, out var level) ? level : 1,
                Score = skillVector[index]
            })
            .GroupBy(x => x.Level)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Score).ThenBy(x => x.Skill, StringComparer.OrdinalIgnoreCase)
                .Take(_topSkillsPerLevel)
                .Select(x => x.Skill)
                .ToList());

        IReadOnlyList<string> GetLevel(int level) => skillScores.TryGetValue(level, out var list) ? list : Array.Empty<string>();

        return new SkillPlanDto(
            jobCode,
            GetLevel(1),
            GetLevel(2),
            GetLevel(3),
            GetLevel(4));
    }

    private static int? ParseLikert(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static double[] Multiply(double[] vector, double[,] matrix)
    {
        var rows = matrix.GetLength(0);
        var cols = matrix.GetLength(1);

        if (vector.Length != rows)
        {
            throw new InvalidOperationException("طول بردار با ماتریس سازگار نیست.");
        }

        var result = new double[cols];
        for (var col = 0; col < cols; col++)
        {
            double sum = 0;
            for (var row = 0; row < rows; row++)
            {
                sum += vector[row] * matrix[row, col];
            }

            result[col] = sum;
        }

        return result;
    }

    private static double[] GetRow(double[,] matrix, int rowIndex)
    {
        var cols = matrix.GetLength(1);
        var result = new double[cols];

        for (var col = 0; col < cols; col++)
        {
            result[col] = matrix[rowIndex, col];
        }

        return result;
    }

    private static double[] Add(double[] left, double[] right)
    {
        if (left.Length != right.Length)
        {
            throw new InvalidOperationException("بردارها هم‌طول نیستند.");
        }

        var result = new double[left.Length];
        for (var i = 0; i < left.Length; i++)
        {
            result[i] = left[i] + right[i];
        }

        return result;
    }

    private static double[] Scale(double[] vector, double factor)
    {
        var result = new double[vector.Length];
        for (var i = 0; i < vector.Length; i++)
        {
            result[i] = vector[i] * factor;
        }

        return result;
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

    private sealed record CliftonResult(double[] ValuesArray, IReadOnlyDictionary<string, double> Dictionary);

    private sealed record PvqResult(double[] ValuesArray, IReadOnlyDictionary<string, double> Dictionary);
}
