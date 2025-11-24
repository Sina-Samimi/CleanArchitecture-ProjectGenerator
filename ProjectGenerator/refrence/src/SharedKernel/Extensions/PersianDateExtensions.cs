using System;
using System.Globalization;
using System.Text;

namespace Arsis.SharedKernel.Extensions;

public static class PersianDateExtensions
{
    private static readonly PersianCalendar Calendar = new();

    private static readonly string[] PersianDigits =
    {
        "۰",
        "۱",
        "۲",
        "۳",
        "۴",
        "۵",
        "۶",
        "۷",
        "۸",
        "۹"
    };

    public static string ToPersianDateString(this DateTimeOffset value)
    {
        var local = value.ToLocalTime();
        return ConvertToPersianDigits(
            $"{Calendar.GetYear(local.DateTime):0000}/{Calendar.GetMonth(local.DateTime):00}/{Calendar.GetDayOfMonth(local.DateTime):00}");
    }

    public static string ToPersianDateTimeString(this DateTimeOffset value)
    {
        var local = value.ToLocalTime();
        var date = $"{Calendar.GetYear(local.DateTime):0000}/{Calendar.GetMonth(local.DateTime):00}/{Calendar.GetDayOfMonth(local.DateTime):00}";
        var time = $"{local.Hour:00}:{local.Minute:00}";
        return ConvertToPersianDigits($"{time} {date}");
    }

    public static string? ToPersianDateString(this DateTimeOffset? value)
    {
        return value.HasValue ? value.Value.ToPersianDateString() : null;
    }

    public static string? ToPersianDateTimeString(this DateTimeOffset? value)
    {
        return value.HasValue ? value.Value.ToPersianDateTimeString() : null;
    }

    public static string ToPersianDateString(this DateTime value)
    {
        return ConvertToPersianDigits(
            $"{Calendar.GetYear(value):0000}/{Calendar.GetMonth(value):00}/{Calendar.GetDayOfMonth(value):00}");
    }

    public static string ToPersianDateTimeString(this DateTime value)
    {
        var date = $"{Calendar.GetYear(value):0000}/{Calendar.GetMonth(value):00}/{Calendar.GetDayOfMonth(value):00}";
        var time = $"{Calendar.GetHour(value):00}:{Calendar.GetMinute(value):00}";
        return ConvertToPersianDigits($"{time} {date}");
    }

    public static string? ToPersianDateString(this DateTime? value)
    {
        return value.HasValue ? value.Value.ToPersianDateString() : null;
    }

    public static string? ToPersianDateTimeString(this DateTime? value)
    {
        return value.HasValue ? value.Value.ToPersianDateTimeString() : null;
    }

    private static string ConvertToPersianDigits(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character is >= '0' and <= '9')
            {
                builder.Append(PersianDigits[character - '0']);
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
