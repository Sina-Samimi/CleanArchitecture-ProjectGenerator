using System;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;
using Arsis.Domain.ValueObjects;

namespace Arsis.Domain.Entities;

public sealed class TalentScore : Entity
{
    public Guid TalentId { get; private set; }

    public Guid UserId { get; private set; }

    public Score Score { get; private set; }

    public DateTimeOffset CalculatedAt { get; private set; }

    [SetsRequiredMembers]
    private TalentScore()
    {
        Score = Score.FromDecimal(0);
    }

    [SetsRequiredMembers]
    public TalentScore(Guid talentId, Guid userId, Score score, DateTimeOffset calculatedAt)
    {
        TalentId = talentId;
        UserId = userId;
        Score = score;
        CalculatedAt = calculatedAt;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateScore(Score score, DateTimeOffset at)
    {
        Score = score;
        CalculatedAt = at;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
