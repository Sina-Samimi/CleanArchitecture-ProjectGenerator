using System;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;
using Arsis.Domain.Enums;

namespace Arsis.Domain.Entities;

public sealed class Question : Entity
{
    private readonly List<Guid> _talentIds = new();

    public string Text { get; private set; }

    public IReadOnlyCollection<Guid> TalentIds => _talentIds.AsReadOnly();

    public LikertScale DefaultScale { get; }

    [SetsRequiredMembers]
    private Question()
    {
        Text = string.Empty;
        DefaultScale = LikertScale.Neutral;
    }

    [SetsRequiredMembers]
    public Question(string text, IEnumerable<Guid> talentIds, LikertScale defaultScale = LikertScale.Neutral)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Question text cannot be empty", nameof(text));
        }

        Text = text.Trim();
        _talentIds.AddRange(talentIds ?? Array.Empty<Guid>());
        DefaultScale = defaultScale;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void AttachTalent(Guid talentId)
    {
        if (!_talentIds.Contains(talentId))
        {
            _talentIds.Add(talentId);
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }
}
