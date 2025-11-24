using Arsis.Application.Assessments;
using Arsis.Application.DTOs.OrganizationAnalysis;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Tests;
using Arsis.Domain.Enums;
using Arsis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Arsis.Infrastructure.Services;

public class OrganizationAnalysisService : IOrganizationAnalysisService
{
    private const string CliftonSchwartzResultType = "CliftonSchwartzResponse";

    private readonly AppDbContext _dbContext;
    private readonly ILogger<OrganizationAnalysisService> _logger;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrganizationAnalysisService(
        AppDbContext dbContext,
        ILogger<OrganizationAnalysisService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WeakUserDto>> GetWeakUsersAsync(Guid organizationId, string weaknessType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetWeakUsersAsync called for organization {OrganizationId} with weakness {WeaknessType}",
            organizationId, weaknessType);

        if (string.IsNullOrWhiteSpace(weaknessType))
        {
            return Array.Empty<WeakUserDto>();
        }

        var attempts = await GetLatestCliftonSchwartzAttemptsAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (attempts.Count == 0)
        {
            return Array.Empty<WeakUserDto>();
        }

        var normalizedWeakness = weaknessType.Trim();
        var comparer = StringComparer.OrdinalIgnoreCase;
        var result = new List<WeakUserDto>();

        foreach (var attempt in attempts)
        {
            if (attempt.Analysis is null)
            {
                continue;
            }

            var weaknesses = ExtractWeaknesses(attempt.Analysis);
            if (weaknesses.Any(w => comparer.Equals(w, normalizedWeakness)))
            {
                var (firstName, lastName) = SplitFullName(attempt.FullName);
                var lastDate = (attempt.CompletedAt ?? attempt.StartedAt).UtcDateTime;
                result.Add(new WeakUserDto(
                    attempt.UserId,
                    firstName,
                    lastName,
                    attempt.AttemptId,
                    attempt.TestId,
                    lastDate));
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<OrganizationWeaknessDto>> GetOrganizationWeaknessesAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetOrganizationWeaknessesAsync called for organization {OrganizationId}", organizationId);

        var attempts = await GetLatestCliftonSchwartzAttemptsAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (attempts.Count == 0)
        {
            return Array.Empty<OrganizationWeaknessDto>();
        }

        var counter = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var attempt in attempts)
        {
            if (attempt.Analysis is null)
            {
                continue;
            }

            foreach (var weakness in ExtractWeaknesses(attempt.Analysis))
            {
                if (counter.TryGetValue(weakness, out var count))
                {
                    counter[weakness] = count + 1;
                }
                else
                {
                    counter[weakness] = 1;
                }
            }
        }

        return counter
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new OrganizationWeaknessDto(pair.Key, pair.Value))
            .ToList();
    }

    public async Task<IReadOnlyList<UserTestResultDto>> GetUserTestResultsAsync(Guid organizationId, string jobGroup, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetUserTestResultsAsync called for organization {OrganizationId} with jobGroup {JobGroup}",
            organizationId, jobGroup);

        if (string.IsNullOrWhiteSpace(jobGroup))
        {
            return Array.Empty<UserTestResultDto>();
        }

        var attempts = await GetLatestCliftonSchwartzAttemptsAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (attempts.Count == 0)
        {
            return Array.Empty<UserTestResultDto>();
        }

        var comparer = StringComparer.OrdinalIgnoreCase;
        var normalizedJobGroup = jobGroup.Trim();
        var results = new List<UserTestResultDto>();

        foreach (var attempt in attempts)
        {
            if (attempt.Analysis is null)
            {
                continue;
            }

            if (TryGetJobGroupScore(attempt.Analysis, normalizedJobGroup, comparer, out var score, out var matchedJobGroup))
            {
                var (firstName, lastName) = SplitFullName(attempt.FullName);
                var completedAt = attempt.CompletedAt ?? attempt.StartedAt;

                results.Add(new UserTestResultDto(
                    attempt.UserId,
                    firstName,
                    lastName,
                    completedAt.UtcDateTime,
                    attempt.AttemptId,
                    attempt.TestId,
                    matchedJobGroup,
                    score));
            }
        }

        return results
            .OrderByDescending(r => r.Result)
            .ThenBy(r => r.TestDateTime)
            .ToList();
    }

    public Task<IReadOnlyList<JobApplicationDto>> GetJobOffersAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetJobOffersAsync called for user {UserId}", userId);

        // Job offers have not been migrated from the Python project yet.
        return Task.FromResult<IReadOnlyList<JobApplicationDto>>(Array.Empty<JobApplicationDto>());
    }

    #region Helpers

    private async Task<IReadOnlyList<AttemptAnalysis>> GetLatestCliftonSchwartzAttemptsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            _logger.LogWarning("OrganizationId was empty. Returning analysis for all users.");
        }

        var attempts = await _dbContext.UserTestAttempts
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Test)
            .Where(a => a.Status == TestAttemptStatus.Completed)
            .Where(a => a.Test.Type == TestType.CliftonSchwartz)
            // TODO: Filter by organization membership once the relationship is modelled.
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (attempts.Count == 0)
        {
            return Array.Empty<AttemptAnalysis>();
        }

        var latestAttempts = attempts
            .GroupBy(a => a.UserId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(a => a.CompletedAt ?? a.StartedAt)
                .ThenByDescending(a => a.AttemptNumber)
                .First())
            .ToList();

        var attemptIds = latestAttempts.Select(a => a.Id).ToList();

        var resultPayloads = await _dbContext.TestResults
            .AsNoTracking()
            .Where(result => attemptIds.Contains(result.AttemptId) && result.ResultType == CliftonSchwartzResultType)
            .Select(result => new { result.AttemptId, result.AdditionalData })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var payloadLookup = resultPayloads
            .Where(r => !string.IsNullOrWhiteSpace(r.AdditionalData))
            .ToDictionary(r => r.AttemptId, r => r.AdditionalData!, EqualityComparer<Guid>.Default);

        var analyses = new List<AttemptAnalysis>(latestAttempts.Count);

        foreach (var attempt in latestAttempts)
        {
            AssessmentResponse? analysis = null;

            if (payloadLookup.TryGetValue(attempt.Id, out var payload))
            {
                analysis = DeserializeAssessmentResponse(attempt.Id, payload);
            }
            else
            {
                _logger.LogWarning("No Clifton + Schwartz result found for attempt {AttemptId}", attempt.Id);
            }

            analyses.Add(new AttemptAnalysis(
                attempt.Id,
                attempt.TestId,
                attempt.UserId,
                attempt.User?.FullName ?? attempt.User?.UserName ?? string.Empty,
                attempt.StartedAt,
                attempt.CompletedAt,
                analysis));
        }

        return analyses;
    }

    private AssessmentResponse? DeserializeAssessmentResponse(Guid attemptId, string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<AssessmentResponse>(payload, _serializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize AssessmentResponse for attempt {AttemptId}", attemptId);
            return null;
        }
    }

    private static IReadOnlyList<string> ExtractWeaknesses(AssessmentResponse response, int count = 5)
    {
        if (response?.Scores?.Clifton is null || response.Scores.Clifton.Count == 0)
        {
            return Array.Empty<string>();
        }

        return response.Scores.Clifton
            .OrderBy(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, count))
            .Select(pair => pair.Key)
            .ToList();
    }

    private static bool TryGetJobGroupScore(
        AssessmentResponse response,
        string jobGroup,
        IEqualityComparer<string> comparer,
        out double score,
        out string matchedJobGroup)
    {
        score = default;
        matchedJobGroup = jobGroup;

        if (response.JobGroups is null)
        {
            return false;
        }

        foreach (var pair in response.JobGroups)
        {
            if (comparer.Equals(pair.Key, jobGroup))
            {
                score = pair.Value;
                matchedJobGroup = pair.Key;
                return true;
            }
        }

        return false;
    }

    private static (string FirstName, string LastName) SplitFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (string.Empty, string.Empty);
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        if (parts.Length == 1)
        {
            return (parts[0], string.Empty);
        }

        return (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private sealed record AttemptAnalysis(
        Guid AttemptId,
        Guid TestId,
        string UserId,
        string FullName,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt,
        AssessmentResponse? Analysis);

    #endregion
}
