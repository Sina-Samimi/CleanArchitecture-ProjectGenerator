using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TestAttarClone.SharedKernel.Authorization;

public static class PermissionGroupUtility
{
    public static string NormalizeGroupKey(string? groupKey)
    {
        if (string.IsNullOrWhiteSpace(groupKey))
        {
            return "custom";
        }

        var trimmed = groupKey.Trim();

        var catalogMatch = PermissionCatalog.Groups.FirstOrDefault(group =>
            string.Equals(group.Key, trimmed, StringComparison.OrdinalIgnoreCase));
        if (catalogMatch is not null)
        {
            return catalogMatch.Key;
        }

        if (string.Equals(trimmed, "custom", StringComparison.OrdinalIgnoreCase))
        {
            return "custom";
        }

        var slug = CreateSlug(trimmed);
        return string.IsNullOrWhiteSpace(slug) ? "custom" : slug;
    }

    public static string ResolveGroupDisplayName(string? groupKey, string? fallbackLabel = null)
    {
        if (string.IsNullOrWhiteSpace(groupKey))
        {
            return fallbackLabel ?? string.Empty;
        }

        var catalogMatch = PermissionCatalog.Groups.FirstOrDefault(group =>
            string.Equals(group.Key, groupKey, StringComparison.OrdinalIgnoreCase));
        if (catalogMatch is not null)
        {
            return catalogMatch.DisplayName;
        }

        if (string.Equals(groupKey, "custom", StringComparison.OrdinalIgnoreCase))
        {
            return "مجوزهای سفارشی";
        }

        return fallbackLabel ?? groupKey;
    }

    private static string CreateSlug(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            if (char.IsWhiteSpace(character) || character is '-' or '_' or '.')
            {
                if (builder.Length > 0 && builder[^1] != '-')
                {
                    builder.Append('-');
                }

                continue;
            }

            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.DecimalDigitNumber or UnicodeCategory.LetterNumber)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        var slug = builder.ToString().Trim('-');
        if (slug.Length > 64)
        {
            slug = slug[..64];
        }

        return slug.Trim('-');
    }
}
