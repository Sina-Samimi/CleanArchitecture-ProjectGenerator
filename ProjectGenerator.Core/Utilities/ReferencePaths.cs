using System;
using System.IO;
using System.Linq;

namespace ProjectGenerator.Core.Utilities;

internal static class ReferencePaths
{
    private static bool LooksLikeMainprojectRoot(string mainprojectDir)
    {
        // User requirement: source of truth is ProjectGenerator/wwwroot/mainproject and it contains these folders.
        return Directory.Exists(Path.Combine(mainprojectDir, "Domain")) &&
               Directory.Exists(Path.Combine(mainprojectDir, "Application")) &&
               Directory.Exists(Path.Combine(mainprojectDir, "Infrastructure")) &&
               Directory.Exists(Path.Combine(mainprojectDir, "SharedKernel"));
    }

    private static bool HasAnyCsproj(string projectDir)
    {
        if (!Directory.Exists(projectDir))
        {
            return false;
        }

        return Directory.EnumerateFiles(projectDir, "*.csproj", SearchOption.TopDirectoryOnly).Any();
    }

    private static string? FindValidWebProjectUnder(string mainprojectDir)
    {
        if (!Directory.Exists(mainprojectDir))
        {
            return null;
        }

        // Only accept a folder that looks like the full mainproject template root,
        // to avoid stale/partial copies in bin output directories.
        if (!LooksLikeMainprojectRoot(mainprojectDir))
        {
            return null;
        }

        // Prefer folders that actually contain a csproj at their root.
        // This avoids picking stale/partial copies that only have subfolders.
        var candidates = Directory.EnumerateDirectories(mainprojectDir)
            .Where(d => d.EndsWith(".WebSite", StringComparison.OrdinalIgnoreCase))
            .Where(HasAnyCsproj)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        // Prefer a candidate where the csproj matches the directory name.
        foreach (var dir in candidates)
        {
            var dirName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(dirName))
            {
                continue;
            }

            var expectedCsproj = Path.Combine(dir, $"{dirName}.csproj");
            if (File.Exists(expectedCsproj))
            {
                return dir;
            }
        }

        // Otherwise just return the first valid candidate.
        return candidates[0];
    }

    /// <summary>
    /// Finds the root folder of the reference Web project that should be cloned.
    ///
    /// ONLY supports the mainproject layout: ProjectGenerator/wwwroot/mainproject/{AnyName}.WebSite
    ///
    /// The returned path is always the web-project root (the folder that contains
    /// the .csproj and Areas/Views/...).
    /// </summary>
    public static string? FindReferenceProjectRoot()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;

        for (var i = 0; i < 8 && !string.IsNullOrEmpty(current); i++)
        {
            // ONLY use mainproject layout: ProjectGenerator/wwwroot/mainproject/{AnyName}.WebSite
            // Dynamically find any .WebSite folder in mainproject
            var mainprojectDirect = Path.Combine(current, "wwwroot", "mainproject");
            var directWeb = FindValidWebProjectUnder(mainprojectDirect);
            if (directWeb != null)
            {
                // Verify that mainproject structure exists (Domain, Application, Infrastructure, SharedKernel)
                var mainprojectRoot = Directory.GetParent(directWeb)?.FullName;
                if (mainprojectRoot != null && IsMainprojectStructureValid(mainprojectRoot))
                {
                    return directWeb;
                }
            }

            var mainprojectSibling = Path.Combine(current, "ProjectGenerator", "wwwroot", "mainproject");
            var siblingWeb = FindValidWebProjectUnder(mainprojectSibling);
            if (siblingWeb != null)
            {
                // Verify that mainproject structure exists (Domain, Application, Infrastructure, SharedKernel)
                var mainprojectRoot = Directory.GetParent(siblingWeb)?.FullName;
                if (mainprojectRoot != null && IsMainprojectStructureValid(mainprojectRoot))
                {
                    return siblingWeb;
                }
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

    private static bool IsMainprojectStructureValid(string mainprojectRoot)
    {
        if (!Directory.Exists(mainprojectRoot))
        {
            return false;
        }

        // Verify that all required folders exist
        var requiredFolders = new[] { "Domain", "Application", "Infrastructure", "SharedKernel" };
        foreach (var folder in requiredFolders)
        {
            var folderPath = Path.Combine(mainprojectRoot, folder);
            if (!Directory.Exists(folderPath))
            {
                return false;
            }

            // Verify that each folder has its .csproj file
            var csprojPath = Path.Combine(folderPath, $"{folder}.csproj");
            if (!File.Exists(csprojPath))
            {
                return false;
            }
        }

        return true;
    }
}
