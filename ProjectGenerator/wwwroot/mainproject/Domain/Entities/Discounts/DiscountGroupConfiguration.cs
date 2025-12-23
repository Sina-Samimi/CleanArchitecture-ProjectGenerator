using MobiRooz.Domain.Base;
using MobiRooz.Domain.Enums;

namespace MobiRooz.Domain.Entities.Discounts;

public sealed class DiscountGroupConfiguration : ValueObject
{
    public DiscountGroupConfiguration(
        string key,
        int? usageLimit = null,
        DiscountType? discountTypeOverride = null,
        decimal? discountValueOverride = null,
        decimal? maxDiscountAmountOverride = null,
        decimal? minimumOrderAmountOverride = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Group key cannot be empty", nameof(key));
        }

        if (usageLimit is not null && usageLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(usageLimit), "Usage limit must be greater than zero.");
        }

        if (discountValueOverride is not null && discountValueOverride <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discountValueOverride), "Discount value override must be greater than zero.");
        }

        if (discountTypeOverride is not null && discountValueOverride is null)
        {
            throw new ArgumentException(
                "Discount value override is required when overriding the discount type.",
                nameof(discountValueOverride));
        }

        if (maxDiscountAmountOverride is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDiscountAmountOverride), "Maximum discount amount override cannot be negative.");
        }

        if (minimumOrderAmountOverride is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumOrderAmountOverride), "Minimum order amount override cannot be negative.");
        }

        Key = key.Trim();
        UsageLimit = usageLimit;
        DiscountTypeOverride = discountTypeOverride;
        DiscountValueOverride = discountValueOverride;
        MaxDiscountAmountOverride = maxDiscountAmountOverride;
        MinimumOrderAmountOverride = minimumOrderAmountOverride;
    }

    public string Key { get; }

    public int? UsageLimit { get; }

    public DiscountType? DiscountTypeOverride { get; }

    public decimal? DiscountValueOverride { get; }

    public decimal? MaxDiscountAmountOverride { get; }

    public decimal? MinimumOrderAmountOverride { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Key;
        yield return UsageLimit;
        yield return DiscountTypeOverride;
        yield return DiscountValueOverride;
        yield return MaxDiscountAmountOverride;
        yield return MinimumOrderAmountOverride;
    }
}
