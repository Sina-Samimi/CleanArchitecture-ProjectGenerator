using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arsis.Application.Commands.Discounts;
using Arsis.Application.DTOs.Discounts;
using Arsis.Application.Queries.Discounts;
using Arsis.Domain.Enums;
using EndPoint.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class DiscountCodesController : Controller
{
    private readonly IMediator _mediator;

    public DiscountCodesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetDiscountCodeListQuery(), cancellationToken);

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Error))
        {
            TempData["Error"] = result.Error;
        }

        var discountDtos = result.IsSuccess && result.Value is not null
            ? result.Value
            : Array.Empty<DiscountCodeDetailDto>();

        var now = DateTimeOffset.UtcNow;
        var items = discountDtos
            .Select(dto => MapToListItem(dto, now))
            .ToArray();

        var summary = new DiscountCodeSummaryViewModel
        {
            TotalCodes = items.Length,
            ActiveCodes = items.Count(item => item.IsCurrentlyActive),
            ScheduledCodes = items.Count(item => item.IsScheduled),
            ExpiredCodes = items.Count(item => item.IsExpired),
            PercentageCodes = items.Count(item => item.DiscountType == DiscountType.Percentage),
            FixedAmountCodes = items.Count(item => item.DiscountType == DiscountType.FixedAmount),
            GroupRestrictedCodes = items.Count(item => item.GroupRules.Count > 0),
            LimitedUsageCodes = items.Count(item => item.GlobalUsageLimit is not null)
        };

        var viewModel = new DiscountCodeIndexViewModel
        {
            Summary = summary,
            Items = items,
            GeneratedAt = now
        };

        ViewData["Title"] = "کدهای تخفیف";
        ViewData["Subtitle"] = "مدیریت کوپن‌های فروشگاه، زمان‌بندی و محدودیت‌های مصرف";

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var now = DateTime.Now;
        var model = new DiscountCodeFormViewModel
        {
            StartsAt = now,
            IsActive = true,
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10
        };

        PopulateScheduleFields(model);
        return PartialView("_DiscountCodeModal", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["Error"] = "شناسه کد تخفیف معتبر نیست.";
            return RedirectToAction(nameof(Index));
        }

        var cancellationToken = HttpContext.RequestAborted;
        var result = await _mediator.Send(new GetDiscountCodeDetailsQuery(id), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(result.Error)
                ? "کد تخفیف مورد نظر یافت نشد."
                : result.Error;
            return RedirectToAction(nameof(Index));
        }

        var model = MapToFormModel(result.Value);
        PopulateScheduleFields(model);
        return PartialView("_DiscountCodeModal", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(DiscountCodeFormViewModel model)
    {
        NormalizeFormModel(model);

        var scheduleResolved = TryResolveSchedule(model);

        if (!scheduleResolved || !ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return PartialView("_DiscountCodeModal", model);
        }

        var cancellationToken = HttpContext.RequestAborted;

        if (model.Id is null || model.Id == Guid.Empty)
        {
            var createCommand = BuildCreateCommand(model);
            var createResult = await _mediator.Send(createCommand, cancellationToken);

            if (!createResult.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, createResult.Error ?? "در ثبت کد تخفیف خطایی رخ داد.");
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_DiscountCodeModal", model);
            }

            TempData["Success"] = "کد تخفیف جدید با موفقیت ثبت شد.";
        }
        else
        {
            var updateCommand = BuildUpdateCommand(model);
            var updateResult = await _mediator.Send(updateCommand, cancellationToken);

            if (!updateResult.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, updateResult.Error ?? "در به‌روزرسانی کد تخفیف خطایی رخ داد.");
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return PartialView("_DiscountCodeModal", model);
            }

            TempData["Success"] = "کد تخفیف با موفقیت به‌روزرسانی شد.";
        }

        return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
    }

    private static DiscountCodeListItemViewModel MapToListItem(DiscountCodeDetailDto dto, DateTimeOffset now)
    {
        var isWithinSchedule = dto.StartsAt <= now && (dto.EndsAt is null || dto.EndsAt >= now);
        var isScheduled = dto.StartsAt > now;
        var isExpired = dto.EndsAt is not null && dto.EndsAt < now;
        var isCurrentlyActive = dto.IsActive && isWithinSchedule && !isExpired;

        var groupRules = dto.GroupRules
            .Select(rule => new DiscountGroupRuleViewModel
            {
                Key = rule.Key,
                UsageLimit = rule.UsageLimit,
                UsedCount = rule.UsedCount,
                RemainingUses = rule.RemainingUses,
                DiscountTypeOverride = rule.DiscountTypeOverride,
                DiscountValueOverride = rule.DiscountValueOverride,
                MaxDiscountAmountOverride = rule.MaxDiscountAmountOverride,
                MinimumOrderAmountOverride = rule.MinimumOrderAmountOverride
            })
            .ToArray();

        return new DiscountCodeListItemViewModel
        {
            Id = dto.Id,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            MinimumOrderAmount = dto.MinimumOrderAmount,
            IsActive = dto.IsActive,
            StartsAt = dto.StartsAt,
            EndsAt = dto.EndsAt,
            GlobalUsageLimit = dto.GlobalUsageLimit,
            RemainingGlobalUses = dto.RemainingGlobalUses,
            TotalRedemptions = dto.TotalRedemptions,
            GroupRules = groupRules,
            IsCurrentlyActive = isCurrentlyActive,
            IsScheduled = isScheduled,
            IsExpired = isExpired
        };
    }

    private static DiscountCodeFormViewModel MapToFormModel(DiscountCodeDetailDto dto)
    {
        var model = new DiscountCodeFormViewModel
        {
            Id = dto.Id,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            MinimumOrderAmount = dto.MinimumOrderAmount,
            IsActive = dto.IsActive,
            StartsAt = dto.StartsAt.LocalDateTime,
            EndsAt = dto.EndsAt?.LocalDateTime,
            GlobalUsageLimit = dto.GlobalUsageLimit,
            GroupRules = dto.GroupRules
                .Select(rule => new DiscountGroupRuleInputViewModel
                {
                    Key = rule.Key,
                    UsageLimit = rule.UsageLimit,
                    DiscountTypeOverride = rule.DiscountTypeOverride,
                    DiscountValueOverride = rule.DiscountValueOverride,
                    MaxDiscountAmountOverride = rule.MaxDiscountAmountOverride,
                    MinimumOrderAmountOverride = rule.MinimumOrderAmountOverride
                })
                .ToList()
        };

        PopulateScheduleFields(model);
        return model;
    }

    private static void NormalizeFormModel(DiscountCodeFormViewModel model)
    {
        model.Code = model.Code?.Trim() ?? string.Empty;
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

        model.StartsAtPersian = model.StartsAtPersian?.Trim();
        model.EndsAtPersian = model.EndsAtPersian?.Trim();
        model.StartsAtTime = model.StartsAtTime?.Trim();
        model.EndsAtTime = model.EndsAtTime?.Trim();

        if (model.GroupRules is null)
        {
            model.GroupRules = new List<DiscountGroupRuleInputViewModel>();
            return;
        }

        foreach (var rule in model.GroupRules)
        {
            if (rule is null)
            {
                continue;
            }

            rule.Key = rule.Key?.Trim() ?? string.Empty;
        }
    }

    private static CreateDiscountCodeCommand BuildCreateCommand(DiscountCodeFormViewModel model)
    {
        var startsAt = SpecifyLocal(model.StartsAt);
        var endsAt = model.EndsAt is null ? (DateTimeOffset?)null : SpecifyLocal(model.EndsAt.Value);

        var groupRules = model.GroupRules
            .Select(rule => new CreateDiscountCodeCommand.GroupRule(
                rule.Key,
                rule.UsageLimit,
                rule.DiscountTypeOverride,
                rule.DiscountValueOverride,
                rule.MaxDiscountAmountOverride,
                rule.MinimumOrderAmountOverride))
            .ToArray();

        return new CreateDiscountCodeCommand(
            model.Code,
            model.Name,
            model.Description,
            model.DiscountType,
            model.DiscountValue,
            startsAt,
            endsAt,
            model.MaxDiscountAmount,
            model.MinimumOrderAmount,
            model.IsActive,
            model.GlobalUsageLimit,
            groupRules);
    }

    private static UpdateDiscountCodeCommand BuildUpdateCommand(DiscountCodeFormViewModel model)
    {
        var startsAt = SpecifyLocal(model.StartsAt);
        var endsAt = model.EndsAt is null ? (DateTimeOffset?)null : SpecifyLocal(model.EndsAt.Value);

        var groupRules = model.GroupRules
            .Select(rule => new UpdateDiscountCodeCommand.GroupRule(
                rule.Key,
                rule.UsageLimit,
                rule.DiscountTypeOverride,
                rule.DiscountValueOverride,
                rule.MaxDiscountAmountOverride,
                rule.MinimumOrderAmountOverride))
            .ToArray();

        return new UpdateDiscountCodeCommand(
            model.Id!.Value,
            model.Code,
            model.Name,
            model.Description,
            model.DiscountType,
            model.DiscountValue,
            startsAt,
            endsAt,
            model.MaxDiscountAmount,
            model.MinimumOrderAmount,
            model.IsActive,
            model.GlobalUsageLimit,
            groupRules);
    }

    private static DateTimeOffset SpecifyLocal(DateTime value)
    {
        var local = DateTime.SpecifyKind(value, DateTimeKind.Local);
        return new DateTimeOffset(local);
    }

    private static void PopulateScheduleFields(DiscountCodeFormViewModel model)
    {
        if (model is null)
        {
            return;
        }

        var calendar = new PersianCalendar();

        if (model.StartsAt != default)
        {
            var startLocal = DateTime.SpecifyKind(model.StartsAt, DateTimeKind.Local);
            var startDate = FormatPersianDate(calendar, startLocal);

            if (string.IsNullOrWhiteSpace(model.StartsAtPersian))
            {
                model.StartsAtPersian = startDate;
            }

            if (string.IsNullOrWhiteSpace(model.StartsAtTime))
            {
                model.StartsAtTime = startLocal.ToString("HH:mm", CultureInfo.InvariantCulture);
            }
        }
        else
        {
            model.StartsAtPersian ??= string.Empty;
            model.StartsAtTime ??= string.Empty;
        }

        if (model.EndsAt is not null)
        {
            var endLocal = DateTime.SpecifyKind(model.EndsAt.Value, DateTimeKind.Local);
            var endDate = FormatPersianDate(calendar, endLocal);

            if (string.IsNullOrWhiteSpace(model.EndsAtPersian))
            {
                model.EndsAtPersian = endDate;
            }

            if (string.IsNullOrWhiteSpace(model.EndsAtTime))
            {
                model.EndsAtTime = endLocal.ToString("HH:mm", CultureInfo.InvariantCulture);
            }
        }
        else
        {
            model.EndsAtPersian ??= string.Empty;
            model.EndsAtTime ??= string.Empty;
        }
    }

    private bool TryResolveSchedule(DiscountCodeFormViewModel model)
    {
        var scheduleValid = true;

        var rawStartDate = model.StartsAtPersian;
        var sanitizedStartDate = SanitizeDateInput(rawStartDate);
        var startNormalizedDate = NormalizePersianDateInput(rawStartDate);
        var startNormalizedTime = NormalizeTimeInput(model.StartsAtTime, allowEmpty: false, out var startHour, out var startMinute, out var startTimeError);
        var sanitizedStartTime = SanitizeTimeInput(model.StartsAtTime);

        model.StartsAtPersian = string.IsNullOrEmpty(startNormalizedDate) ? sanitizedStartDate : startNormalizedDate;
        model.StartsAtTime = string.IsNullOrEmpty(startNormalizedTime) ? sanitizedStartTime : startNormalizedTime;

        if (!string.IsNullOrEmpty(startTimeError))
        {
            ModelState.AddModelError(nameof(DiscountCodeFormViewModel.StartsAtTime), startTimeError);
            scheduleValid = false;
        }

        if (string.IsNullOrEmpty(startNormalizedDate))
        {
            var message = string.IsNullOrWhiteSpace(rawStartDate)
                ? "تاریخ شروع اعتبار را وارد کنید."
                : "تاریخ شروع اعتبار وارد شده معتبر نیست.";
            ModelState.AddModelError(nameof(DiscountCodeFormViewModel.StartsAtPersian), message);
            scheduleValid = false;
        }
        else if (!TryCreateGregorianDateTime(startNormalizedDate, startHour, startMinute, out var startDateTime, out var startError))
        {
            ModelState.AddModelError(nameof(DiscountCodeFormViewModel.StartsAtPersian), startError ?? "تاریخ شروع اعتبار وارد شده معتبر نیست.");
            scheduleValid = false;
        }
        else
        {
            model.StartsAt = startDateTime;
        }

        var rawEndDate = model.EndsAtPersian;
        var sanitizedEndDate = SanitizeDateInput(rawEndDate);
        var endNormalizedDate = NormalizePersianDateInput(rawEndDate);
        var endNormalizedTime = NormalizeTimeInput(model.EndsAtTime, allowEmpty: true, out var endHour, out var endMinute, out var endTimeError);
        var sanitizedEndTime = SanitizeTimeInput(model.EndsAtTime);

        model.EndsAtPersian = string.IsNullOrEmpty(endNormalizedDate) ? sanitizedEndDate : endNormalizedDate;
        model.EndsAtTime = string.IsNullOrEmpty(endNormalizedTime) ? sanitizedEndTime : endNormalizedTime;

        if (!string.IsNullOrEmpty(endTimeError))
        {
            ModelState.AddModelError(nameof(DiscountCodeFormViewModel.EndsAtTime), endTimeError);
            scheduleValid = false;
        }

        if (string.IsNullOrEmpty(endNormalizedDate))
        {
            if (!string.IsNullOrWhiteSpace(rawEndDate) || !string.IsNullOrWhiteSpace(endNormalizedTime))
            {
                var message = string.IsNullOrWhiteSpace(rawEndDate)
                    ? "برای تعیین پایان اعتبار، تاریخ را وارد کنید."
                    : "تاریخ پایان اعتبار وارد شده معتبر نیست.";
                ModelState.AddModelError(nameof(DiscountCodeFormViewModel.EndsAtPersian), message);
                scheduleValid = false;
            }

            model.EndsAt = null;
            return scheduleValid;
        }

        if (!TryCreateGregorianDateTime(endNormalizedDate, endHour, endMinute, out var endDateTime, out var endError))
        {
            ModelState.AddModelError(nameof(DiscountCodeFormViewModel.EndsAtPersian), endError ?? "تاریخ پایان اعتبار وارد شده معتبر نیست.");
            scheduleValid = false;
        }
        else
        {
            model.EndsAt = endDateTime;
        }

        return scheduleValid;
    }

    private static string NormalizePersianDateInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedDigits = NormalizeDigits(value)
            .Replace("\u200f", string.Empty, StringComparison.Ordinal)
            .Replace("\u200e", string.Empty, StringComparison.Ordinal)
            .Replace(".", "/", StringComparison.Ordinal)
            .Replace("-", "/", StringComparison.Ordinal)
            .Replace("\\", "/", StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        var parts = normalizedDigits.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return string.Empty;
        }

        var year = parts[0].PadLeft(4, '0');
        var month = parts[1].PadLeft(2, '0');
        var day = parts[2].PadLeft(2, '0');

        return string.Create(10, (year, month, day), static (span, state) =>
        {
            var (y, m, d) = state;
            y.AsSpan().CopyTo(span);
            span[4] = '-';
            m.AsSpan().CopyTo(span[5..]);
            span[7] = '-';
            d.AsSpan().CopyTo(span[8..]);
        });
    }

    private static string NormalizeTimeInput(string? value, bool allowEmpty, out int hour, out int minute, out string? error)
    {
        error = null;
        hour = 0;
        minute = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return allowEmpty ? string.Empty : "00:00";
        }

        var sanitized = NormalizeDigits(value)
            .Replace("\u200f", string.Empty, StringComparison.Ordinal)
            .Replace("\u200e", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(".", ":", StringComparison.Ordinal)
            .Replace("-", ":", StringComparison.Ordinal)
            .Trim();

        if (sanitized.Length is 3 or 4 && !sanitized.Contains(':', StringComparison.Ordinal))
        {
            var insertIndex = sanitized.Length - 2;
            sanitized = sanitized.Insert(insertIndex, ":");
        }

        if (!TimeSpan.TryParseExact(
                sanitized,
                new[] { "hh\\:mm", "h\\:mm", "HH\\:mm", "H\\:mm" },
                CultureInfo.InvariantCulture,
                out var timeSpan))
        {
            error = allowEmpty ? "ساعت پایان وارد شده معتبر نیست." : "ساعت شروع وارد شده معتبر نیست.";
            return string.Empty;
        }

        if (timeSpan.TotalHours >= 24)
        {
            error = allowEmpty ? "ساعت پایان وارد شده معتبر نیست." : "ساعت شروع وارد شده معتبر نیست.";
            return string.Empty;
        }

        hour = timeSpan.Hours;
        minute = timeSpan.Minutes;

        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", hour, minute);
    }

    private static bool TryCreateGregorianDateTime(string value, int hour, int minute, out DateTime result, out string? error)
    {
        result = default;
        error = null;

        if (!TryExtractPersianDateParts(value, out var year, out var month, out var day))
        {
            error = "تاریخ انتخاب‌شده معتبر نیست.";
            return false;
        }

        try
        {
            var persianDateTime = new global::PersianDateTime(year, month, day, hour, minute, 0);
            var gregorian = persianDateTime.ToDateTime();
            result = DateTime.SpecifyKind(gregorian, DateTimeKind.Unspecified);
            return true;
        }
        catch
        {
            error = "تاریخ انتخاب‌شده معتبر نیست.";
            return false;
        }
    }

    private static bool TryExtractPersianDateParts(string value, out int year, out int month, out int day)
    {
        year = 0;
        month = 0;
        day = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out year))
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out month))
        {
            return false;
        }

        if (!int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out day))
        {
            return false;
        }

        return true;
    }

    private static string NormalizeDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '۰' => '0',
                '۱' => '1',
                '۲' => '2',
                '۳' => '3',
                '۴' => '4',
                '۵' => '5',
                '۶' => '6',
                '۷' => '7',
                '۸' => '8',
                '۹' => '9',
                '٠' => '0',
                '١' => '1',
                '٢' => '2',
                '٣' => '3',
                '٤' => '4',
                '٥' => '5',
                '٦' => '6',
                '٧' => '7',
                '٨' => '8',
                '٩' => '9',
                _ => character
            });
        }

        return builder.ToString();
    }

    private static string FormatPersianDate(PersianCalendar calendar, DateTime dateTime)
    {
        var year = calendar.GetYear(dateTime);
        var month = calendar.GetMonth(dateTime);
        var day = calendar.GetDayOfMonth(dateTime);

        return string.Create(10, (year, month, day), static (span, state) =>
        {
            var (y, m, d) = state;
            span[0] = (char)('0' + (y / 1000) % 10);
            span[1] = (char)('0' + (y / 100) % 10);
            span[2] = (char)('0' + (y / 10) % 10);
            span[3] = (char)('0' + y % 10);
            span[4] = '-';
            span[5] = (char)('0' + (m / 10) % 10);
            span[6] = (char)('0' + m % 10);
            span[7] = '-';
            span[8] = (char)('0' + (d / 10) % 10);
            span[9] = (char)('0' + d % 10);
        });
    }

    private static string SanitizeDateInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return NormalizeDigits(value)
            .Replace("\u200f", string.Empty, StringComparison.Ordinal)
            .Replace("\u200e", string.Empty, StringComparison.Ordinal)
            .Replace(".", "/", StringComparison.Ordinal)
            .Replace("-", "/", StringComparison.Ordinal)
            .Replace("\\", "/", StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static string SanitizeTimeInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = NormalizeDigits(value)
            .Replace("\u200f", string.Empty, StringComparison.Ordinal)
            .Replace("\u200e", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(".", ":", StringComparison.Ordinal)
            .Replace("-", ":", StringComparison.Ordinal)
            .Trim();

        if (sanitized.Length is 3 or 4 && !sanitized.Contains(':', StringComparison.Ordinal))
        {
            var insertIndex = sanitized.Length - 2;
            sanitized = sanitized.Insert(insertIndex, ":");
        }

        return sanitized;
    }
}
