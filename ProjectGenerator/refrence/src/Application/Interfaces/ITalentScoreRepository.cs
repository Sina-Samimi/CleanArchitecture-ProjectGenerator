using Arsis.Domain.Entities;

namespace Arsis.Application.Interfaces;

public interface ITalentScoreRepository
{
    Task SaveScoresAsync(IEnumerable<TalentScore> scores, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TalentScore>> GetTopScoresAsync(Guid userId, int count, CancellationToken cancellationToken);
}
