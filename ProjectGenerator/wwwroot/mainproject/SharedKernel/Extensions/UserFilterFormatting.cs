using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MobiRooz.SharedKernel.Helpers;

namespace MobiRooz.SharedKernel.Extensions;

public static class UserFilterFormatting
{
    private static readonly string[] PersianMonthNames =
    {
        "فروردین",
        "اردیبهشت",
        "خرداد",
        "تیر",
        "مرداد",
        "شهریور",
        "مهر",
        "آبان",
        "آذر",
        "دی",
        "بهمن",
        "اسفند"
    };

    public static string GetPersianMonthName(int month)
    {
        if (month < 1 || month > PersianMonthNames.Length)
        {
            return string.Empty;
        }

        return PersianMonthNames[month - 1];
    }

    public static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static string? NormalizePhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digitsOnly = PhoneNumberHelper.ExtractDigits(value);
        return string.IsNullOrEmpty(digitsOnly) ? null : digitsOnly;
    }

    public static DateTimeOffset? ParsePersianDate(string? value, bool toExclusiveEnd, out string? normalized)
    {
        normalized = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var parts = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return null;
        }

        if (!int.TryParse(parts[0], out var year) ||
            !int.TryParse(parts[1], out var month) ||
            !int.TryParse(parts[2], out var day))
        {
            return null;
        }

        normalized = $"{year:D4}-{month:D2}-{day:D2}";

        try
        {
            var calendar = new PersianCalendar();
            var dateTime = calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
            var offset = new DateTimeOffset(unspecified, TimeSpan.Zero);
            return toExclusiveEnd ? offset.AddDays(1) : offset;
        }
        catch
        {
            normalized = null;
            return null;
        }
    }

    public static string? BuildPersianDateDisplay(string? normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return null;
        }

        if (!int.TryParse(parts[0], out var year) ||
            !int.TryParse(parts[1], out var month) ||
            !int.TryParse(parts[2], out var day))
        {
            return null;
        }

        if (month < 1 || month > PersianMonthNames.Length)
        {
            return null;
        }

        return $"{day} {PersianMonthNames[month - 1]} {year}";
    }

    public static IReadOnlyCollection<string> ParseRoles(string? roles)
    {
        if (string.IsNullOrWhiteSpace(roles))
        {
            return Array.Empty<string>();
        }

        var parsed = roles
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return parsed.Length == 0 ? Array.Empty<string>() : parsed;
    }
}
