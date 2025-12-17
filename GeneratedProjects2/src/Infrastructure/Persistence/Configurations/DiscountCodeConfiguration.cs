using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using LogsDtoCloneTest.Domain.Entities.Discounts;
using LogsDtoCloneTest.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Configurations;

public sealed class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.ToTable("DiscountCodes");

        builder.HasKey(discount => discount.Id);

        builder.HasIndex(discount => discount.Code)
            .IsUnique();

        builder.Property(discount => discount.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(discount => discount.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(discount => discount.Description)
            .HasMaxLength(1000);

        builder.Property(discount => discount.DiscountType)
            .HasConversion<int>();

        builder.Property(discount => discount.DiscountValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(discount => discount.MaxDiscountAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(discount => discount.MinimumOrderAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(discount => discount.StartsAt)
            .IsRequired();

        builder.Property(discount => discount.EndsAt);

        builder.Property(discount => discount.GlobalUsageLimit);

        builder.Property(discount => discount.TotalRedemptions);

        builder.Ignore(discount => discount.GroupConfigurations);
        builder.Ignore(discount => discount.GroupUsages);

        builder.Property<Dictionary<string, DiscountCode.GroupConfigurationSnapshot>>("GroupConfigurationsData")
            .HasColumnName("GroupConfigurations")
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                value => SerializeGroupConfigurations(value),
                json => DeserializeGroupConfigurations(json))
            .Metadata.SetValueComparer(CreateGroupConfigurationComparer());

        builder.Property<Dictionary<string, int>>("GroupUsagesData")
            .HasColumnName("GroupUsages")
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                value => SerializeGroupUsages(value),
                json => DeserializeGroupUsages(json))
            .Metadata.SetValueComparer(CreateGroupUsageComparer());
    }

    private static string SerializeGroupConfigurations(Dictionary<string, DiscountCode.GroupConfigurationSnapshot> value)
    {
        if (value.Count == 0)
        {
            return string.Empty;
        }

        var payload = value
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Where(pair => pair.Value is not null)
            .Select(pair => new GroupConfigurationData(
                pair.Value!.Key,
                pair.Value!.UsageLimit,
                pair.Value!.DiscountTypeOverride,
                pair.Value!.DiscountValueOverride,
                pair.Value!.MaxDiscountAmountOverride,
                pair.Value!.MinimumOrderAmountOverride))
            .ToArray();

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static Dictionary<string, DiscountCode.GroupConfigurationSnapshot> DeserializeGroupConfigurations(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, DiscountCode.GroupConfigurationSnapshot>(StringComparer.OrdinalIgnoreCase);
        }

        var payload = JsonSerializer.Deserialize<GroupConfigurationData[]>(json, JsonOptions)
            ?? Array.Empty<GroupConfigurationData>();

        var dictionary = new Dictionary<string, DiscountCode.GroupConfigurationSnapshot>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in payload)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                continue;
            }

            var snapshot = new DiscountCode.GroupConfigurationSnapshot(
                item.Key,
                item.UsageLimit,
                item.DiscountTypeOverride,
                item.DiscountValueOverride,
                item.MaxDiscountAmountOverride,
                item.MinimumOrderAmountOverride);

            dictionary[snapshot.Key] = snapshot;
        }

        return dictionary;
    }

    private static string SerializeGroupUsages(Dictionary<string, int> value)
    {
        if (value.Count == 0)
        {
            return string.Empty;
        }

        var payload = value
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new GroupUsageData(pair.Key, pair.Value))
            .ToArray();

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static Dictionary<string, int> DeserializeGroupUsages(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        var payload = JsonSerializer.Deserialize<GroupUsageData[]>(json, JsonOptions)
            ?? Array.Empty<GroupUsageData>();

        var dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in payload)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                continue;
            }

            dictionary[item.Key.Trim()] = item.Usage;
        }

        return dictionary;
    }

    private static ValueComparer<Dictionary<string, DiscountCode.GroupConfigurationSnapshot>> CreateGroupConfigurationComparer()
        => new(
            (left, right) => GroupConfigurationEquals(left, right),
            value => ComputeGroupConfigurationHash(value),
            value => CloneGroupConfigurations(value));

    private static ValueComparer<Dictionary<string, int>> CreateGroupUsageComparer()
        => new(
            (left, right) => left.Count == right.Count &&
                !left.Except(right, GroupUsageComparer.Instance).Any(),
            value => value.Aggregate(
                0,
                (hash, pair) => HashCode.Combine(
                    hash,
                    StringComparer.OrdinalIgnoreCase.GetHashCode(pair.Key),
                    pair.Value)),
            value => value.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase));

    private sealed class GroupConfigurationComparer : IEqualityComparer<KeyValuePair<string, DiscountCode.GroupConfigurationSnapshot>>
    {
        public static readonly GroupConfigurationComparer Instance = new();

        public bool Equals(
            KeyValuePair<string, DiscountCode.GroupConfigurationSnapshot> x,
            KeyValuePair<string, DiscountCode.GroupConfigurationSnapshot> y)
            => StringComparer.OrdinalIgnoreCase.Equals(x.Key, y.Key)
                && EqualityComparer<DiscountCode.GroupConfigurationSnapshot>.Default.Equals(x.Value, y.Value);

        public int GetHashCode(KeyValuePair<string, DiscountCode.GroupConfigurationSnapshot> obj)
            => HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key),
                obj.Value == null ? 0 : obj.Value.GetHashCode());
    }

    private static bool GroupConfigurationEquals(
        Dictionary<string, DiscountCode.GroupConfigurationSnapshot>? left,
        Dictionary<string, DiscountCode.GroupConfigurationSnapshot>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        return !left.Except(right, GroupConfigurationComparer.Instance).Any();
    }

    private static int ComputeGroupConfigurationHash(
        Dictionary<string, DiscountCode.GroupConfigurationSnapshot>? value)
    {
        if (value is null || value.Count == 0)
        {
            return 0;
        }

        var hash = 0;

        foreach (var pair in value)
        {
            if (pair.Value is null)
            {
                continue;
            }

            hash = HashCode.Combine(
                hash,
                StringComparer.OrdinalIgnoreCase.GetHashCode(pair.Key),
                pair.Value.GetHashCode());
        }

        return hash;
    }

    private static Dictionary<string, DiscountCode.GroupConfigurationSnapshot> CloneGroupConfigurations(
        Dictionary<string, DiscountCode.GroupConfigurationSnapshot>? value)
    {
        var clone = new Dictionary<string, DiscountCode.GroupConfigurationSnapshot>(StringComparer.OrdinalIgnoreCase);

        if (value is null || value.Count == 0)
        {
            return clone;
        }

        foreach (var pair in value)
        {
            if (pair.Value is null)
            {
                continue;
            }

            clone[pair.Key] = pair.Value;
        }

        return clone;
    }

    private sealed class GroupUsageComparer : IEqualityComparer<KeyValuePair<string, int>>
    {
        public static readonly GroupUsageComparer Instance = new();

        public bool Equals(KeyValuePair<string, int> x, KeyValuePair<string, int> y)
            => StringComparer.OrdinalIgnoreCase.Equals(x.Key, y.Key) && x.Value == y.Value;

        public int GetHashCode(KeyValuePair<string, int> obj)
            => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key), obj.Value);
    }

    private sealed record GroupConfigurationData(
        string Key,
        int? UsageLimit,
        DiscountType? DiscountTypeOverride,
        decimal? DiscountValueOverride,
        decimal? MaxDiscountAmountOverride,
        decimal? MinimumOrderAmountOverride);

    private sealed record GroupUsageData(string Key, int Usage);
}
