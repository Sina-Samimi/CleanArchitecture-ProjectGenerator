using System.Text;
using System.Text.RegularExpressions;

namespace LogTableRenameTest.SharedKernel.Helpers;

public static class PhoneNumberHelper
{
    // User-provided pattern (accepts separators) simplified for C#
    // Original supplied: /(^(0?9)|(\+?989))\d{2}\W?\d{3}\W?\d{4}/g
    private static readonly Regex PhonePattern = new(@"^(?:(?:0?9)|(?:\+?989))\d{2}\W?\d{3}\W?\d{4}$", RegexOptions.Compiled);

    // Normalized digits-only pattern: +98 or 0 followed by 9 and 9 digits (total 11 digits with leading 0)
    private static readonly Regex NormalizedPattern = new(@"^(?:\+?98|0)9\d{9}$", RegexOptions.Compiled);

    public static bool IsValid(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // First try matching the original flexible pattern (allows separators)
        if (PhonePattern.IsMatch(input))
            return true;

        // Fallback: normalize to digits-only and test normalized pattern
        var digits = ExtractDigits(input);
        if (string.IsNullOrEmpty(digits))
            return false;

        // Normalize common international prefixes like 0098 or 98 -> 0
        var normalized = digits;
        if (normalized.StartsWith("0098", StringComparison.Ordinal))
        {
            normalized = "0" + normalized[4..];
        }
        else if (normalized.StartsWith("98", StringComparison.Ordinal) && normalized.Length > 2)
        {
            normalized = "0" + normalized[2..];
        }

        return NormalizedPattern.IsMatch(normalized);
    }

    public static string? ExtractDigits(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (char.IsDigit(ch)) sb.Append(ch);
        }

        return sb.Length == 0 ? null : sb.ToString();
    }
}
