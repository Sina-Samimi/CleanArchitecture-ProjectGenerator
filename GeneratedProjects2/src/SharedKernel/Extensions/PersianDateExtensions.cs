using System;
using System.Globalization;
using System.Text;

namespace LogsDtoCloneTest.SharedKernel.Extensions;

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

    public static DateTimeOffset GetPersianMonthStart(this DateTimeOffset value)
    {
        var local = value.ToLocalTime();
        var year = Calendar.GetYear(local.DateTime);
        var month = Calendar.GetMonth(local.DateTime);
        var firstDayOfMonth = Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
        return new DateTimeOffset(firstDayOfMonth, TimeSpan.Zero);
    }

    public static DateTimeOffset GetPersianMonthEnd(this DateTimeOffset value)
    {
        var local = value.ToLocalTime();
        var year = Calendar.GetYear(local.DateTime);
        var month = Calendar.GetMonth(local.DateTime);
        var daysInMonth = Calendar.GetDaysInMonth(year, month);
        var lastDayOfMonth = Calendar.ToDateTime(year, month, daysInMonth, 23, 59, 59, 999);
        return new DateTimeOffset(lastDayOfMonth, TimeSpan.Zero);
    }

    public static DateOnly GetPersianWeekStart(this DateOnly value)
    {
        var dateTime = value.ToDateTime(TimeOnly.MinValue);
        var dayOfWeek = dateTime.DayOfWeek;
        
        // Convert DayOfWeek to Persian week day (Saturday = 0, Friday = 6)
        var persianDayOfWeek = dayOfWeek switch
        {
            DayOfWeek.Saturday => 0,
            DayOfWeek.Sunday => 1,
            DayOfWeek.Monday => 2,
            DayOfWeek.Tuesday => 3,
            DayOfWeek.Wednesday => 4,
            DayOfWeek.Thursday => 5,
            DayOfWeek.Friday => 6,
            _ => 0
        };
        
        var daysToSubtract = persianDayOfWeek;
        var weekStartDateTime = dateTime.AddDays(-daysToSubtract);
        return DateOnly.FromDateTime(weekStartDateTime);
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
