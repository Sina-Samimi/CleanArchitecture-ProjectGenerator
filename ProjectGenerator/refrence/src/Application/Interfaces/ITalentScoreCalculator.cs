using Arsis.Domain.Entities;

namespace Arsis.Application.Interfaces;

public interface ITalentScoreCalculator
{
    Task<IReadOnlyCollection<TalentScore>> CalculateAsync(
        Guid userId,
        IReadOnlyCollection<Question> questions,
        IReadOnlyCollection<Talent> talents,
        IReadOnlyCollection<UserResponse> responses,
        AuditMetadata audit,
        CancellationToken cancellationToken);
}
