namespace TestAttarClone.WebSite.Helpers;

public static class StringHelper
{
    /// <summary>
    /// Truncates a string to the specified maximum length and appends "..." if truncated.
    /// </summary>
    /// <param name="text">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the string.</param>
    /// <returns>The truncated string with "..." appended if necessary, or the original string if it's shorter than maxLength.</returns>
    public static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength).TrimEnd() + "...";
    }

    /// <summary>
    /// Truncates a description string to 100 characters by default.
    /// </summary>
    /// <param name="description">The description to truncate.</param>
    /// <param name="maxLength">The maximum length (default: 100).</param>
    /// <returns>The truncated description.</returns>
    public static string TruncateDescription(string? description, int maxLength = 100)
    {
        return Truncate(description, maxLength);
    }

    /// <summary>
    /// Truncates a name string to 50 characters by default.
    /// </summary>
    /// <param name="name">The name to truncate.</param>
    /// <param name="maxLength">The maximum length (default: 50).</param>
    /// <returns>The truncated name.</returns>
    public static string TruncateName(string? name, int maxLength = 50)
    {
        return Truncate(name, maxLength);
    }
}

