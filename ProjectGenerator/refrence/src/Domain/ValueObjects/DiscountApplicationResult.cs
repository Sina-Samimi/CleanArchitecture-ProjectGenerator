using Arsis.Domain.Base;
using Arsis.Domain.Enums;

namespace Arsis.Domain.ValueObjects;

public sealed class DiscountApplicationResult : ValueObject
{
    public DiscountApplicationResult(
        string code,
        DiscountType appliedDiscountType,
        decimal appliedDiscountValue,
        decimal originalPrice,
        decimal discountAmount,
        string? audienceKey,
        bool wasCapped,
        DateTimeOffset evaluatedAt,
        decimal? maxDiscountAmount)
    {
        Code = code;
        AppliedDiscountType = appliedDiscountType;
        AppliedDiscountValue = appliedDiscountValue;
        OriginalPrice = decimal.Round(originalPrice, 2, MidpointRounding.AwayFromZero);
        DiscountAmount = decimal.Round(discountAmount, 2, MidpointRounding.AwayFromZero);
        AudienceKey = string.IsNullOrWhiteSpace(audienceKey) ? null : audienceKey.Trim();
        WasCapped = wasCapped;
        EvaluatedAt = evaluatedAt;
        MaxDiscountAmount = maxDiscountAmount is null
            ? null
            : decimal.Round(maxDiscountAmount.Value, 2, MidpointRounding.AwayFromZero);
    }

    public string Code { get; }

    public DiscountType AppliedDiscountType { get; }

    public decimal AppliedDiscountValue { get; }

    public decimal OriginalPrice { get; }

    public decimal DiscountAmount { get; }

    public decimal FinalPrice => OriginalPrice - DiscountAmount;

    public string? AudienceKey { get; }

    public bool WasCapped { get; }

    public DateTimeOffset EvaluatedAt { get; }

    public decimal? MaxDiscountAmount { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return AppliedDiscountType;
        yield return AppliedDiscountValue;
        yield return OriginalPrice;
        yield return DiscountAmount;
        yield return AudienceKey;
        yield return WasCapped;
        yield return EvaluatedAt;
        yield return MaxDiscountAmount;
    }
}
