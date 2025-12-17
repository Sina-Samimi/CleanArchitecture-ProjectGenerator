using System;
using System.Collections.Generic;
using System.Linq;
using LogTableRenameTest.Domain.Entities.Discounts;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.Domain.ValueObjects;
using static LogTableRenameTest.Domain.Entities.Discounts.DiscountCode;

namespace LogTableRenameTest.Application.DTOs.Discounts;

public sealed record DiscountApplicationResultDto(
    string Code,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal OriginalPrice,
    decimal DiscountAmount,
    decimal FinalPrice,
    bool WasCapped,
    string? AudienceKey,
    DateTimeOffset EvaluatedAt,
    decimal? MaxDiscountAmount);

public sealed record DiscountGroupRuleDto(
    string Key,
    int? UsageLimit,
    int UsedCount,
    int? RemainingUses,
    DiscountType? DiscountTypeOverride,
    decimal? DiscountValueOverride,
    decimal? MaxDiscountAmountOverride,
    decimal? MinimumOrderAmountOverride);

public sealed record DiscountCodeDetailDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal? MinimumOrderAmount,
    bool IsActive,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    int? GlobalUsageLimit,
    int? RemainingGlobalUses,
    int TotalRedemptions,
    IReadOnlyCollection<DiscountGroupRuleDto> GroupRules);

public static class DiscountDtoMapper
{
    public static DiscountApplicationResultDto ToDto(this DiscountApplicationResult result)
        => new(
            result.Code,
            result.AppliedDiscountType,
            result.AppliedDiscountValue,
            result.OriginalPrice,
            result.DiscountAmount,
            result.FinalPrice,
            result.WasCapped,
            result.AudienceKey,
            result.EvaluatedAt,
            result.MaxDiscountAmount);

    public static DiscountCodeDetailDto ToDetailDto(this DiscountCode discountCode)
    {
        var groupDtos = discountCode.GroupConfigurations
            .Select(configuration =>
            {
                discountCode.TryGetGroupUsage(configuration.Key, out var usage);
                var remaining = discountCode.GetRemainingUsesForGroup(configuration.Key);
                return new DiscountGroupRuleDto(
                    configuration.Key,
                    configuration.UsageLimit,
                    usage,
                    remaining,
                    configuration.DiscountTypeOverride,
                    configuration.DiscountValueOverride,
                    configuration.MaxDiscountAmountOverride,
                    configuration.MinimumOrderAmountOverride);
            })
            .ToArray();

        return new DiscountCodeDetailDto(
            discountCode.Id,
            discountCode.Code,
            discountCode.Name,
            discountCode.Description,
            discountCode.DiscountType,
            discountCode.DiscountValue,
            discountCode.MaxDiscountAmount,
            discountCode.MinimumOrderAmount,
            discountCode.IsActive,
            discountCode.StartsAt,
            discountCode.EndsAt,
            discountCode.GlobalUsageLimit,
            discountCode.RemainingGlobalUses,
            discountCode.TotalRedemptions,
            groupDtos);
    }
}
