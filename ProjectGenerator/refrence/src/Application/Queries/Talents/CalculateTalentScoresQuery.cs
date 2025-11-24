using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;
using Arsis.SharedKernel.Constants;

namespace Arsis.Application.Queries.Talents;

public sealed record CalculateTalentScoresQuery(Guid UserId) : IQuery<IReadOnlyCollection<TalentScoreDto>>
{
    public sealed class Handler : IQueryHandler<CalculateTalentScoresQuery, IReadOnlyCollection<TalentScoreDto>>
    {
        private readonly ITalentScoreRepository _talentScoreRepository;
        private readonly ITalentRepository _talentRepository;

        public Handler(ITalentScoreRepository talentScoreRepository, ITalentRepository talentRepository)
        {
            _talentScoreRepository = talentScoreRepository;
            _talentRepository = talentRepository;
        }

        public async Task<Result<IReadOnlyCollection<TalentScoreDto>>> Handle(CalculateTalentScoresQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                return Result<IReadOnlyCollection<TalentScoreDto>>.Failure("User id cannot be empty.");
            }

            var scores = await _talentScoreRepository.GetTopScoresAsync(request.UserId, TestConstants.DefaultTopTalentCount, cancellationToken);
            var talents = await _talentRepository.GetAllAsync(cancellationToken);

            var talentLookup = talents.ToDictionary(t => t.Id);
            var dtos = scores
                .Select(score => new TalentScoreDto(
                    score.TalentId,
                    talentLookup.TryGetValue(score.TalentId, out var talent) ? talent.Name : "Unknown",
                    score.Score.Value))
                .ToList();

            return Result<IReadOnlyCollection<TalentScoreDto>>.Success(dtos);
        }
    }
}
