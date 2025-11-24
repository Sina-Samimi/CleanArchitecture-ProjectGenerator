using Arsis.Application.Assessments;

namespace Arsis.Infrastructure.Services.Assessments;

public sealed class WeightedRatioStrategy : IScoringStrategy
{
    public double Score(IEnumerable<int> responses, int maxLikert)
    {
        ArgumentNullException.ThrowIfNull(responses);
        var values = responses.Where(r => r >= 0).ToArray();
        if (values.Length == 0)
        {
            return 0d;
        }

        var weights = Enumerable.Range(1, values.Length).Select(i => (double)i).ToArray();
        var numerator = values.Zip(weights, (value, weight) => value * weight).Sum();
        var max = Math.Max(1, maxLikert);
        var denominator = weights.Sum() * max;
        return Math.Round(numerator / denominator, 6, MidpointRounding.AwayFromZero);
    }
}
