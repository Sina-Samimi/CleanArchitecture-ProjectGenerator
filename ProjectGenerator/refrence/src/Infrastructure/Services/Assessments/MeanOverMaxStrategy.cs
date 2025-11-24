using Arsis.Application.Assessments;

namespace Arsis.Infrastructure.Services.Assessments;

public sealed class MeanOverMaxStrategy : IScoringStrategy
{
    public double Score(IEnumerable<int> responses, int maxLikert)
    {
        ArgumentNullException.ThrowIfNull(responses);
        var list = responses.Where(r => r >= 0).ToList();
        if (list.Count == 0)
        {
            return 0d;
        }

        var max = Math.Max(1, maxLikert);
        var average = list.Average();
        return Math.Round(average / max, 6, MidpointRounding.AwayFromZero);
    }
}
