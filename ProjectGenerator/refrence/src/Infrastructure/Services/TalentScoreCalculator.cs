using Arsis.Application.Interfaces;
using Arsis.Domain.Entities;
using Arsis.Domain.ValueObjects;

namespace Arsis.Infrastructure.Services;

public sealed class TalentScoreCalculator : ITalentScoreCalculator
{
    public Task<IReadOnlyCollection<TalentScore>> CalculateAsync(
        Guid userId,
        IReadOnlyCollection<Question> questions,
        IReadOnlyCollection<Talent> talents,
        IReadOnlyCollection<UserResponse> responses,
        AuditMetadata audit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(talents);
        ArgumentNullException.ThrowIfNull(responses);

        var responseLookup = responses.ToDictionary(response => response.QuestionId);
        var result = new List<TalentScore>();

        foreach (var talent in talents)
        {
            var relatedQuestions = questions.Where(question => question.TalentIds.Contains(talent.Id)).ToList();
            if (relatedQuestions.Count == 0)
            {
                continue;
            }

            var values = new List<decimal>();
            foreach (var question in relatedQuestions)
            {
                if (!responseLookup.TryGetValue(question.Id, out var response))
                {
                    continue;
                }

                values.Add(MapLikertToScore(response.Answer));
            }

            if (values.Count == 0)
            {
                continue;
            }

            var average = values.Average();
            var talentScore = new TalentScore(
                talent.Id,
                userId,
                Score.FromDecimal(average),
                DateTimeOffset.UtcNow)
            {
                CreatorId = audit.UserId,
                CreateDate = audit.Timestamp,
                UpdateDate = audit.Timestamp,
                Ip = audit.IpAddress
            };

            result.Add(talentScore);
        }

        return Task.FromResult<IReadOnlyCollection<TalentScore>>(result);
    }

    private static decimal MapLikertToScore(Arsis.Domain.Enums.LikertScale scale)
        => ((int)scale - 1) / 4m * 100m;
}
