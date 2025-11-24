using System;
using System.IO;

namespace ProjectGenerator.Core.Utilities;

internal static class ReferencePaths
{
    public static string? FindReferenceProjectRoot()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;

        for (var i = 0; i < 8 && !string.IsNullOrEmpty(current); i++)
        {
            var direct = Path.Combine(current, "refrence", "EndPoint.WebSite");
            if (Directory.Exists(direct))
            {
                return direct;
            }

            var sibling = Path.Combine(current, "ProjectGenerator", "refrence", "EndPoint.WebSite");
            if (Directory.Exists(sibling))
            {
                return sibling;
            }

            var parent = Directory.GetParent(current);
            if (parent == null)
            {
                break;
            }
            current = parent.FullName;
        }

        return null;
    }
}

