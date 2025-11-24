namespace Arsis.Application.Assessments;

public interface IScoringStrategy
{
    double Score(IEnumerable<int> responses, int maxLikert);
}

public interface IScoringStrategyResolver
{
    IScoringStrategy Resolve(string strategyKey);
}
