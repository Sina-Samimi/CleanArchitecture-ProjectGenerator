using System;
using System.Linq;

namespace MobiRooz.SharedKernel.Helpers;

public static class ReferenceGenerator
{
    private static readonly char[] Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
    private static readonly Random Random = new();

    /// <summary>
    /// Generates a short, readable, unique reference code.
    /// Format: PREFIX-YYYYMMDD-XXXXXX
    /// Example: PAY-20251128-A3B9C2
    /// </summary>
    public static string GenerateReadableReference(string prefix, DateTimeOffset? timestamp = null)
    {
        var date = timestamp ?? DateTimeOffset.UtcNow;
        var datePart = date.ToString("yyyyMMdd");
        
        // Generate 6-character random alphanumeric code
        var randomPart = new string(Enumerable.Range(0, 6)
            .Select(_ => Base62Chars[Random.Next(Base62Chars.Length)])
            .ToArray());
        
        return $"{prefix.ToUpperInvariant()}-{datePart}-{randomPart}";
    }

    /// <summary>
    /// Generates a short reference code without date.
    /// Format: PREFIX-XXXXXX
    /// Example: REF-A3B9C2
    /// </summary>
    public static string GenerateShortReference(string prefix)
    {
        var randomPart = new string(Enumerable.Range(0, 6)
            .Select(_ => Base62Chars[Random.Next(Base62Chars.Length)])
            .ToArray());
        
        return $"{prefix.ToUpperInvariant()}-{randomPart}";
    }
}

