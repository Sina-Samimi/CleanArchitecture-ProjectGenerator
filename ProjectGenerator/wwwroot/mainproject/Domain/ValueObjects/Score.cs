using MobiRooz.Domain.Base;
using MobiRooz.Domain.Exceptions;

namespace MobiRooz.Domain.ValueObjects;

public sealed class Score : ValueObject
{
    public const decimal MinValue = 0m;
    public const decimal MaxValue = 100m;

    public decimal Value { get; }

    private Score(decimal value)
    {
        Value = Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    public static Score FromDecimal(decimal value)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new InvalidScoreException(value);
        }

        return new Score(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("0.##");

    public static implicit operator decimal(Score score) => score.Value;
}
