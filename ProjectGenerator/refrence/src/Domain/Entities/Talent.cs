using System;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;
using Arsis.Domain.Interfaces;

namespace Arsis.Domain.Entities;

public sealed class Talent : Entity, IAggregateRoot
{
    private readonly List<TalentScore> _scores = new();

    public string Name { get; private set; }

    public string Description { get; private set; }

    public IReadOnlyCollection<TalentScore> Scores => _scores.AsReadOnly();

    [SetsRequiredMembers]
    private Talent()
    {
        Name = string.Empty;
        Description = string.Empty;
    }

    [SetsRequiredMembers]
    public Talent(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Talent name cannot be empty", nameof(name));
        }

        Name = name.Trim();
        Description = description.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateDescription(string description)
    {
        Description = description.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void AddScore(TalentScore score)
    {
        ArgumentNullException.ThrowIfNull(score);
        _scores.Add(score);
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
