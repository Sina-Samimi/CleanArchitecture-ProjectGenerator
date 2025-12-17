using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.WebSite.Areas.Admin.Models;

public sealed class DiscountCodeIndexViewModel
{
    public DiscountCodeSummaryViewModel Summary { get; init; } = new();

    public IReadOnlyCollection<DiscountCodeListItemViewModel> Items { get; init; }
        = Array.Empty<DiscountCodeListItemViewModel>();

    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class DiscountCodeSummaryViewModel
{
    public int TotalCodes { get; init; }

    public int ActiveCodes { get; init; }

    public int ScheduledCodes { get; init; }

    public int ExpiredCodes { get; init; }

    public int PercentageCodes { get; init; }

    public int FixedAmountCodes { get; init; }

    public int GroupRestrictedCodes { get; init; }

    public int LimitedUsageCodes { get; init; }
}

public sealed class DiscountCodeListItemViewModel
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public DiscountType DiscountType { get; init; }

    public decimal DiscountValue { get; init; }

    public decimal? MaxDiscountAmount { get; init; }

    public decimal? MinimumOrderAmount { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset StartsAt { get; init; }

    public DateTimeOffset? EndsAt { get; init; }

    public int? GlobalUsageLimit { get; init; }

    public int? RemainingGlobalUses { get; init; }

    public int TotalRedemptions { get; init; }

    public IReadOnlyCollection<DiscountGroupRuleViewModel> GroupRules { get; init; }
        = Array.Empty<DiscountGroupRuleViewModel>();

    public bool IsCurrentlyActive { get; init; }

    public bool IsScheduled { get; init; }

    public bool IsExpired { get; init; }
}

public sealed class DiscountGroupRuleViewModel
{
    public string Key { get; init; } = string.Empty;

    public int? UsageLimit { get; init; }

    public int UsedCount { get; init; }

    public int? RemainingUses { get; init; }

    public DiscountType? DiscountTypeOverride { get; init; }

    public decimal? DiscountValueOverride { get; init; }

    public decimal? MaxDiscountAmountOverride { get; init; }

    public decimal? MinimumOrderAmountOverride { get; init; }
}

public sealed class DiscountCodeFormViewModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Display(Name = "کد تخفیف")]
    [Required(ErrorMessage = "کد تخفیف را وارد کنید.")]
    [StringLength(64, ErrorMessage = "کد تخفیف حداکثر {1} کاراکتر باشد.")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "عنوان نمایشی")]
    [Required(ErrorMessage = "عنوان کد تخفیف را وارد کنید.")]
    [StringLength(128, ErrorMessage = "عنوان می‌تواند حداکثر {1} کاراکتر باشد.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "توضیحات")]
    [StringLength(512, ErrorMessage = "توضیحات می‌تواند حداکثر {1} کاراکتر باشد.")]
    public string? Description { get; set; }

    [Display(Name = "نوع تخفیف")]
    [Required(ErrorMessage = "نوع تخفیف را انتخاب کنید.")]
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

    [Display(Name = "مقدار تخفیف")]
    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "مقدار تخفیف باید بیشتر از صفر باشد.")]
    public decimal DiscountValue { get; set; } = 10;

    [Display(Name = "سقف مبلغ تخفیف")]
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "سقف تخفیف نمی‌تواند منفی باشد.")]
    public decimal? MaxDiscountAmount { get; set; }

    [Display(Name = "حداقل مبلغ سفارش")]
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "حداقل مبلغ سفارش نمی‌تواند منفی باشد.")]
    public decimal? MinimumOrderAmount { get; set; }

    [Display(Name = "شروع اعتبار")]
    public DateTime StartsAt { get; set; } = DateTime.Now;

    [Display(Name = "تاریخ شروع اعتبار")]
    public string? StartsAtPersian { get; set; }

    [Display(Name = "ساعت شروع")]
    public string? StartsAtTime { get; set; }

    [Display(Name = "پایان اعتبار")]
    public DateTime? EndsAt { get; set; }

    [Display(Name = "تاریخ پایان اعتبار")]
    public string? EndsAtPersian { get; set; }

    [Display(Name = "ساعت پایان")]
    public string? EndsAtTime { get; set; }

    [Display(Name = "فعال باشد")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "سقف مصرف کلی")]
    [Range(1, int.MaxValue, ErrorMessage = "سقف مصرف باید بزرگ‌تر از صفر باشد.")]
    public int? GlobalUsageLimit { get; set; }

    public List<DiscountGroupRuleInputViewModel> GroupRules { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DiscountValue <= 0)
        {
            yield return new ValidationResult(
                "مقدار تخفیف باید بیشتر از صفر باشد.",
                new[] { nameof(DiscountValue) });
        }

        if (StartsAt == default)
        {
            yield return new ValidationResult(
                "تاریخ شروع اعتبار را مشخص کنید.",
                new[] { nameof(StartsAt) });
        }

        if (EndsAt is not null && EndsAt <= StartsAt)
        {
            yield return new ValidationResult(
                "پایان اعتبار باید بعد از زمان شروع باشد.",
                new[] { nameof(EndsAt) });
        }

        if (GroupRules is null || GroupRules.Count == 0)
        {
            yield break;
        }

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < GroupRules.Count; index++)
        {
            var rule = GroupRules[index];
            if (rule is null)
            {
                continue;
            }

            var keyPath = $"{nameof(GroupRules)}[{index}].{nameof(rule.Key)}";

            if (string.IsNullOrWhiteSpace(rule.Key))
            {
                yield return new ValidationResult("شناسه گروه الزامی است.", new[] { keyPath });
            }
            else
            {
                var normalizedKey = rule.Key.Trim();
                if (!seenKeys.Add(normalizedKey))
                {
                    yield return new ValidationResult(
                        "شناسه گروه باید یکتا باشد.",
                        new[] { keyPath });
                }
            }

            if (rule.UsageLimit is not null && rule.UsageLimit <= 0)
            {
                yield return new ValidationResult(
                    "سقف مصرف گروه باید بزرگ‌تر از صفر باشد.",
                    new[] { $"{nameof(GroupRules)}[{index}].{nameof(rule.UsageLimit)}" });
            }

            if (rule.DiscountTypeOverride.HasValue &&
                (rule.DiscountValueOverride is null || rule.DiscountValueOverride <= 0))
            {
                yield return new ValidationResult(
                    "برای تخفیف جایگزین مقدار معتبر وارد کنید.",
                    new[]
                    {
                        $"{nameof(GroupRules)}[{index}].{nameof(rule.DiscountValueOverride)}"
                    });
            }

            if (!rule.DiscountTypeOverride.HasValue &&
                rule.DiscountValueOverride is not null && rule.DiscountValueOverride > 0)
            {
                yield return new ValidationResult(
                    "برای مقدار تخفیف جایگزین نوع تخفیف را مشخص کنید.",
                    new[]
                    {
                        $"{nameof(GroupRules)}[{index}].{nameof(rule.DiscountTypeOverride)}"
                    });
            }

            if (rule.MaxDiscountAmountOverride is not null && rule.MaxDiscountAmountOverride < 0)
            {
                yield return new ValidationResult(
                    "سقف تخفیف گروه نمی‌تواند منفی باشد.",
                    new[]
                    {
                        $"{nameof(GroupRules)}[{index}].{nameof(rule.MaxDiscountAmountOverride)}"
                    });
            }

            if (rule.MinimumOrderAmountOverride is not null && rule.MinimumOrderAmountOverride < 0)
            {
                yield return new ValidationResult(
                    "حداقل مبلغ سفارش گروه نمی‌تواند منفی باشد.",
                    new[]
                    {
                        $"{nameof(GroupRules)}[{index}].{nameof(rule.MinimumOrderAmountOverride)}"
                    });
            }
        }
    }
}

public sealed class DiscountGroupRuleInputViewModel
{
    [Display(Name = "شناسه گروه")]
    [StringLength(64, ErrorMessage = "شناسه گروه می‌تواند حداکثر {1} کاراکتر باشد.")]
    public string Key { get; set; } = string.Empty;

    [Display(Name = "سقف مصرف")]
    [Range(1, int.MaxValue, ErrorMessage = "سقف مصرف باید بزرگ‌تر از صفر باشد.")]
    public int? UsageLimit { get; set; }

    [Display(Name = "نوع تخفیف جایگزین")]
    public DiscountType? DiscountTypeOverride { get; set; }

    [Display(Name = "مقدار تخفیف جایگزین")]
    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "مقدار تخفیف باید بیشتر از صفر باشد.")]
    public decimal? DiscountValueOverride { get; set; }

    [Display(Name = "سقف تخفیف جایگزین")]
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "سقف تخفیف نمی‌تواند منفی باشد.")]
    public decimal? MaxDiscountAmountOverride { get; set; }

    [Display(Name = "حداقل مبلغ سفارش جایگزین")]
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "حداقل مبلغ سفارش نمی‌تواند منفی باشد.")]
    public decimal? MinimumOrderAmountOverride { get; set; }
}
