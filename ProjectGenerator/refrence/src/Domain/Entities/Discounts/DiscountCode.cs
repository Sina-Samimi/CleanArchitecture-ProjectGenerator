using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;
using Arsis.Domain.Enums;
using Arsis.Domain.Exceptions;
using Arsis.Domain.Interfaces;
using Arsis.Domain.ValueObjects;

namespace Arsis.Domain.Entities.Discounts;

public sealed class DiscountCode : Entity, IAggregateRoot
{
    private readonly Dictionary<string, DiscountGroupConfiguration> _groupConfigurations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _groupUsages = new(StringComparer.OrdinalIgnoreCase);

    public string Code { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public DiscountType DiscountType { get; private set; }

    public decimal DiscountValue { get; private set; }

    public decimal? MaxDiscountAmount { get; private set; }

    public decimal? MinimumOrderAmount { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset? EndsAt { get; private set; }

    public bool IsActive { get; private set; }

    public int? GlobalUsageLimit { get; private set; }

    public int TotalRedemptions { get; private set; }

    public int? RemainingGlobalUses => GlobalUsageLimit is null
        ? null
        : Math.Max(GlobalUsageLimit.Value - TotalRedemptions, 0);

    public IReadOnlyCollection<DiscountGroupConfiguration> GroupConfigurations
        => _groupConfigurations.Values.ToList().AsReadOnly();

    public IReadOnlyDictionary<string, int> GroupUsages
        => new ReadOnlyDictionary<string, int>(_groupUsages);

    [SetsRequiredMembers]
    private DiscountCode()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    [SetsRequiredMembers]
    public DiscountCode(
        string code,
        string name,
        string? description,
        DiscountType discountType,
        decimal discountValue,
        DateTimeOffset startsAt,
        DateTimeOffset? endsAt,
        decimal? maxDiscountAmount,
        decimal? minimumOrderAmount,
        bool isActive,
        int? globalUsageLimit,
        IEnumerable<DiscountGroupConfiguration>? groupConfigurations = null)
    {
        SetCode(code);
        UpdateDetails(name, description);
        SetDiscount(discountType, discountValue, maxDiscountAmount, minimumOrderAmount);
        UpdateSchedule(startsAt, endsAt);
        SetGlobalUsageLimit(globalUsageLimit);
        IsActive = isActive;

        if (groupConfigurations is not null)
        {
            foreach (var configuration in groupConfigurations)
            {
                AddOrUpdateGroup(configuration);
            }
        }
    }

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Discount code cannot be empty.");
        }

        Code = code.Trim().ToUpperInvariant();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Discount name cannot be empty.");
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetDiscount(
        DiscountType discountType,
        decimal discountValue,
        decimal? maxDiscountAmount,
        decimal? minimumOrderAmount)
    {
        EnsureValidDiscount(discountType, discountValue);

        if (maxDiscountAmount is < 0)
        {
            throw new DomainException("Maximum discount amount cannot be negative.");
        }

        if (minimumOrderAmount is < 0)
        {
            throw new DomainException("Minimum order amount cannot be negative.");
        }

        DiscountType = discountType;
        DiscountValue = discountType == DiscountType.Percentage
            ? decimal.Round(discountValue, 2, MidpointRounding.AwayFromZero)
            : decimal.Round(discountValue, 2, MidpointRounding.AwayFromZero);
        MaxDiscountAmount = maxDiscountAmount is null
            ? null
            : decimal.Round(maxDiscountAmount.Value, 2, MidpointRounding.AwayFromZero);
        MinimumOrderAmount = minimumOrderAmount is null
            ? null
            : decimal.Round(minimumOrderAmount.Value, 2, MidpointRounding.AwayFromZero);
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateSchedule(DateTimeOffset startsAt, DateTimeOffset? endsAt)
    {
        if (endsAt is not null && endsAt < startsAt)
        {
            throw new DomainException("End date cannot be earlier than the start date.");
        }

        StartsAt = startsAt;
        EndsAt = endsAt;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetGlobalUsageLimit(int? usageLimit)
    {
        if (usageLimit is not null && usageLimit <= 0)
        {
            throw new DomainException("Global usage limit must be greater than zero.");
        }

        if (usageLimit is not null && usageLimit < TotalRedemptions)
        {
            throw new DomainException("Global usage limit cannot be lower than the number of completed redemptions.");
        }

        GlobalUsageLimit = usageLimit;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public bool RemoveGroup(string groupKey)
    {
        if (string.IsNullOrWhiteSpace(groupKey))
        {
            return false;
        }

        var normalizedKey = NormalizeGroupKey(groupKey);
        var removed = _groupConfigurations.Remove(normalizedKey);
        _groupUsages.Remove(normalizedKey);

        if (removed)
        {
            UpdateDate = DateTimeOffset.UtcNow;
        }

        return removed;
    }

    public void ClearGroups()
    {
        _groupConfigurations.Clear();
        _groupUsages.Clear();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void AddOrUpdateGroup(DiscountGroupConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var effectiveType = configuration.DiscountTypeOverride ?? DiscountType;
        var effectiveValue = configuration.DiscountValueOverride ?? DiscountValue;

        EnsureValidDiscount(effectiveType, effectiveValue);

        if (configuration.MaxDiscountAmountOverride is not null && configuration.MaxDiscountAmountOverride < 0)
        {
            throw new DomainException("Group maximum discount amount override cannot be negative.");
        }

        if (configuration.MinimumOrderAmountOverride is not null && configuration.MinimumOrderAmountOverride < 0)
        {
            throw new DomainException("Group minimum order amount override cannot be negative.");
        }

        var normalizedKey = NormalizeGroupKey(configuration.Key);
        var normalizedConfiguration = configuration.Key == normalizedKey
            ? configuration
            : new DiscountGroupConfiguration(
                normalizedKey,
                configuration.UsageLimit,
                configuration.DiscountTypeOverride,
                configuration.DiscountValueOverride,
                configuration.MaxDiscountAmountOverride,
                configuration.MinimumOrderAmountOverride);

        if (_groupUsages.TryGetValue(normalizedKey, out var existingUsage)
            && normalizedConfiguration.UsageLimit is not null
            && existingUsage > normalizedConfiguration.UsageLimit.Value)
        {
            throw new DomainException("Existing usage exceeds the configured usage limit for this group.");
        }

        _groupConfigurations[normalizedKey] = normalizedConfiguration;

        if (!_groupUsages.ContainsKey(normalizedKey))
        {
            _groupUsages.Add(normalizedKey, 0);
        }

        UpdateDate = DateTimeOffset.UtcNow;
    }

    public bool TryGetGroupUsage(string groupKey, out int usage)
    {
        if (string.IsNullOrWhiteSpace(groupKey))
        {
            usage = 0;
            return false;
        }

        var normalizedKey = NormalizeGroupKey(groupKey);
        return _groupUsages.TryGetValue(normalizedKey, out usage);
    }

    public int? GetRemainingUsesForGroup(string groupKey)
    {
        var normalizedKey = NormalizeGroupKey(groupKey);

        if (!_groupConfigurations.TryGetValue(normalizedKey, out var configuration))
        {
            return null;
        }

        if (configuration.UsageLimit is null)
        {
            return null;
        }

        var used = _groupUsages.TryGetValue(normalizedKey, out var usage) ? usage : 0;
        return Math.Max(configuration.UsageLimit.Value - used, 0);
    }

    public void Activate()
    {
        IsActive = true;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void ResetUsage()
    {
        TotalRedemptions = 0;
        _groupUsages.Clear();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public DiscountApplicationResult Preview(
        decimal originalPrice,
        DateTimeOffset evaluationDate,
        string? audienceKey = null)
        => Evaluate(originalPrice, evaluationDate, audienceKey, false);

    public DiscountApplicationResult Redeem(
        decimal originalPrice,
        DateTimeOffset evaluationDate,
        string? audienceKey = null)
        => Evaluate(originalPrice, evaluationDate, audienceKey, true);

#pragma warning disable IDE0051
    private Dictionary<string, GroupConfigurationSnapshot> GroupConfigurationsData
    {
        get
        {
            var snapshots = new Dictionary<string, GroupConfigurationSnapshot>(StringComparer.OrdinalIgnoreCase);

            foreach (var configuration in _groupConfigurations.Values)
            {
                var snapshot = new GroupConfigurationSnapshot(
                    configuration.Key,
                    configuration.UsageLimit,
                    configuration.DiscountTypeOverride,
                    configuration.DiscountValueOverride,
                    configuration.MaxDiscountAmountOverride,
                    configuration.MinimumOrderAmountOverride);

                snapshots[snapshot.Key] = snapshot;
            }

            return snapshots;
        }
        set
        {
            _groupConfigurations.Clear();

            if (value is null)
            {
                return;
            }

            foreach (var pair in value)
            {
                if (pair.Value is null)
                {
                    continue;
                }

                var normalizedKey = NormalizeGroupKey(pair.Value.Key);
                var configuration = new DiscountGroupConfiguration(
                    normalizedKey,
                    pair.Value.UsageLimit,
                    pair.Value.DiscountTypeOverride,
                    pair.Value.DiscountValueOverride,
                    pair.Value.MaxDiscountAmountOverride,
                    pair.Value.MinimumOrderAmountOverride);

                _groupConfigurations[normalizedKey] = configuration;
            }
        }
    }

    private Dictionary<string, int> GroupUsagesData
    {
        get => _groupUsages;
        set
        {
            _groupUsages.Clear();

            if (value is null)
            {
                return;
            }

            foreach (var pair in value)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                var normalizedKey = NormalizeGroupKey(pair.Key);
                _groupUsages[normalizedKey] = pair.Value;
            }
        }
    }
#pragma warning restore IDE0051

    public sealed record GroupConfigurationSnapshot(
        string Key,
        int? UsageLimit,
        DiscountType? DiscountTypeOverride,
        decimal? DiscountValueOverride,
        decimal? MaxDiscountAmountOverride,
        decimal? MinimumOrderAmountOverride);

    private DiscountApplicationResult Evaluate(
        decimal originalPrice,
        DateTimeOffset evaluationDate,
        string? audienceKey,
        bool registerUsage)
    {
        if (originalPrice < 0)
        {
            throw new DomainException("Original price cannot be negative.");
        }

        if (!IsActive)
        {
            throw new DomainException("Discount code is not active.");
        }

        if (evaluationDate < StartsAt || (EndsAt is not null && evaluationDate > EndsAt))
        {
            throw new DomainException("Discount code is not currently valid.");
        }

        if (GlobalUsageLimit is not null && TotalRedemptions >= GlobalUsageLimit.Value)
        {
            throw new DomainException("Discount code usage limit has been reached.");
        }

        var effectiveMinimumOrder = MinimumOrderAmount;
        var normalizedAudienceKey = string.IsNullOrWhiteSpace(audienceKey) ? null : NormalizeGroupKey(audienceKey);
        DiscountGroupConfiguration? configuration = null;

        if (_groupConfigurations.Count > 0)
        {
            if (normalizedAudienceKey is null)
            {
                throw new DomainException("This discount code is limited to specific groups.");
            }

            if (!_groupConfigurations.TryGetValue(normalizedAudienceKey, out configuration))
            {
                throw new DomainException("The provided group is not eligible for this discount code.");
            }

            var groupUsage = _groupUsages.TryGetValue(configuration.Key, out var usage) ? usage : 0;
            if (configuration.UsageLimit is not null && groupUsage >= configuration.UsageLimit.Value)
            {
                throw new DomainException("This group has exhausted its usage limit for this discount code.");
            }

            if (configuration.MinimumOrderAmountOverride is not null)
            {
                effectiveMinimumOrder = configuration.MinimumOrderAmountOverride;
            }
        }

        if (effectiveMinimumOrder is not null && originalPrice < effectiveMinimumOrder.Value)
        {
            throw new DomainException("The order amount does not meet the minimum required for this discount code.");
        }

        var effectiveType = configuration?.DiscountTypeOverride ?? DiscountType;
        var effectiveValue = configuration?.DiscountValueOverride ?? DiscountValue;
        var effectiveMaxDiscount = configuration?.MaxDiscountAmountOverride ?? MaxDiscountAmount;

        EnsureValidDiscount(effectiveType, effectiveValue);

        decimal discountAmount = effectiveType switch
        {
            DiscountType.Percentage => decimal.Round(originalPrice * effectiveValue / 100m, 2, MidpointRounding.AwayFromZero),
            DiscountType.FixedAmount => decimal.Round(effectiveValue, 2, MidpointRounding.AwayFromZero),
            _ => throw new DomainException("Unsupported discount type.")
        };

        var wasCapped = false;

        if (effectiveMaxDiscount is not null && discountAmount > effectiveMaxDiscount.Value)
        {
            discountAmount = effectiveMaxDiscount.Value;
            wasCapped = true;
        }

        if (discountAmount > originalPrice)
        {
            discountAmount = originalPrice;
            wasCapped = true;
        }

        var result = new DiscountApplicationResult(
            Code,
            effectiveType,
            effectiveValue,
            originalPrice,
            discountAmount,
            normalizedAudienceKey,
            wasCapped,
            evaluationDate,
            effectiveMaxDiscount);

        if (registerUsage)
        {
            TotalRedemptions++;
            if (normalizedAudienceKey is not null)
            {
                _groupUsages[normalizedAudienceKey] = _groupUsages.TryGetValue(normalizedAudienceKey, out var usage)
                    ? usage + 1
                    : 1;
            }

            UpdateDate = evaluationDate;
        }

        return result;
    }

    private static void EnsureValidDiscount(DiscountType discountType, decimal discountValue)
    {
        if (discountValue <= 0)
        {
            throw new DomainException("Discount value must be greater than zero.");
        }

        if (discountType == DiscountType.Percentage && discountValue > 100)
        {
            throw new DomainException("Percentage discount cannot exceed 100%.");
        }
    }

    private static string NormalizeGroupKey(string groupKey)
        => groupKey.Trim();
}
