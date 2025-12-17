using System;
using System.IO;

namespace ProjectGenerator.Core.Utilities;

internal static class ReferencePaths
{
    /// <summary>
    /// Finds the root folder of the reference Web project that should be cloned.
    ///
    /// This currently supports two layouts:
    /// 1) Legacy layout:   refrence/EndPoint.WebSite
    /// 2) New mainproject: ProjectGenerator/wwwroot/mainproject/Attar.WebSite
    ///
    /// The returned path is always the web-project root (the folder that contains
    /// the .csproj and Areas/Views/...).
    /// </summary>
    public static string? FindReferenceProjectRoot()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;

        for (var i = 0; i < 8 && !string.IsNullOrEmpty(current); i++)
        {
            // New preferred layout: ProjectGenerator/wwwroot/mainproject/Attar.WebSite
            var newDirect = Path.Combine(current, "wwwroot", "mainproject", "Attar.WebSite");
            if (Directory.Exists(newDirect))
            {
                return newDirect;
            }

            var newSibling = Path.Combine(current, "ProjectGenerator", "wwwroot", "mainproject", "Attar.WebSite");
            if (Directory.Exists(newSibling))
            {
                return newSibling;
            }

            // Legacy layout: refrence/EndPoint.WebSite
            var legacyDirect = Path.Combine(current, "refrence", "EndPoint.WebSite");
            if (Directory.Exists(legacyDirect))
            {
                return legacyDirect;
            }

            var legacySibling = Path.Combine(current, "ProjectGenerator", "refrence", "EndPoint.WebSite");
            if (Directory.Exists(legacySibling))
            {
                return legacySibling;
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
