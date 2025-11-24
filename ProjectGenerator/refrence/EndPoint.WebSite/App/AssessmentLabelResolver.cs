using System;
using System.Collections.Generic;
using System.Linq;
using EndPoint.WebSite.Growth;

namespace EndPoint.WebSite.App;

public static class AssessmentLabelResolver
{
    private static readonly Dictionary<string, string> CliftonMap = BuildCliftonMap();
    private static readonly Dictionary<string, string> PvqMap = BuildPvqMap();

    public static string ResolveClifton(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        code = Normalize(code);
        return CliftonMap.TryGetValue(code, out var label)
            ? label
            : code;
    }

    public static string ResolvePvq(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        code = Normalize(code);
        return PvqMap.TryGetValue(code, out var label)
            ? label
            : code;
    }

    public static string ResolveSkill(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        code = Normalize(code);
        if (code.StartsWith("S", StringComparison.OrdinalIgnoreCase))
        {
            var number = code[1..];
            return $"مهارت {number}";
        }

        return code;
    }

    private static Dictionary<string, string> BuildCliftonMap()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in AssessmentQuestionsData.Clifton)
        {
            AddIfMissing(dict, item.TalentCodeA, item.TextA);
            AddIfMissing(dict, item.TalentCodeB, item.TextB);
        }

        return dict;
    }

    private static Dictionary<string, string> BuildPvqMap()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in AssessmentQuestionsData.PVQ)
        {
            AddIfMissing(dict, item.PvqCode, item.Text);
        }

        return dict;
    }

    private static void AddIfMissing(IDictionary<string, string> map, string? code, string text)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        code = Normalize(code);
        if (map.ContainsKey(code))
        {
            return;
        }

        var label = Shorten(text);
        map[code] = label;
    }

    private static string Shorten(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        var separators = new[] { '،', '.', '؛', '!' };
        var index = trimmed.IndexOfAny(separators);
        if (index > 0)
        {
            trimmed = trimmed[..index];
        }

        if (trimmed.Length > 45)
        {
            trimmed = trimmed[..45] + "…";
        }

        return trimmed;
    }

    private static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}
