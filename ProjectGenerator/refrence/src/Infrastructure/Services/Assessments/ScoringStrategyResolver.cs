using Arsis.Application.Assessments;

namespace Arsis.Infrastructure.Services.Assessments;

public sealed class ScoringStrategyResolver : IScoringStrategyResolver
{
    private readonly IDictionary<string, IScoringStrategy> _strategies;

    public ScoringStrategyResolver(IEnumerable<IScoringStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        _strategies = strategies.ToDictionary(
            strategy => strategy switch
            {
                MeanOverMaxStrategy => "MeanOverMax",
                WeightedRatioStrategy => "WeightedRatio",
                _ => strategy.GetType().Name
            },
            strategy => strategy,
            StringComparer.OrdinalIgnoreCase);
    }

    public IScoringStrategy Resolve(string strategyKey)
    {
        if (string.IsNullOrWhiteSpace(strategyKey))
        {
            strategyKey = "MeanOverMax";
        }

        if (_strategies.TryGetValue(strategyKey, out var strategy))
        {
            return strategy;
        }

        var normalizedKey = strategyKey.Replace("Strategy", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (_strategies.TryGetValue(normalizedKey, out strategy))
        {
            return strategy;
        }

        throw new KeyNotFoundException($"Scoring strategy '{strategyKey}' was not registered.");
    }
}
