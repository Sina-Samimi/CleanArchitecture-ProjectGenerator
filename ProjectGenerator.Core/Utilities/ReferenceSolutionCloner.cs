using ProjectGenerator.Core.Models;
using ProjectGenerator.Core.Templates;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectGenerator.Core.Utilities;

internal static class ReferenceSolutionCloner
{
    private enum ReferenceTemplateKind
    {
        Unknown = 0,
        Arsis = 1,
        Attar = 2
    }

    private static readonly string[] SourceDirectories =
    [
        "Domain",
        "SharedKernel",
        "Application",
        "Infrastructure"
    ];

    private static readonly HashSet<string> SkippedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        ".git",
        ".vs",
        // NOTE: we intentionally do NOT add "logs" here because some valid
        // source folders (like Application/DTOs/Logs) would be skipped.
        // Runtime log folders are filtered explicitly in ShouldSkip instead.
        "TestResults"
    };

    // Directories and files related to Test/Assessment/Report that should be excluded
    private static readonly HashSet<string> TestRelatedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        // Application Layer
        "Commands/SubmitTest",
        "Commands/Tests",
        "Commands/Reports",
        "Queries/Tests",
        "Queries/OrganizationAnalysis",
        "Queries/Talents",
        "DTOs/OrganizationAnalysis",
        "Assessments",
        // Application Interfaces
        "Interfaces/IAssessmentQuestionRepository.cs",
        "Interfaces/IAssessmentService.cs",
        "Interfaces/IQuestionRepository.cs",
        "Interfaces/ITalentRepository.cs",
        "Interfaces/ITalentScoreCalculator.cs",
        "Interfaces/ITalentScoreRepository.cs",
        "Interfaces/ITestRepository.cs",
        "Interfaces/ITestResultRepository.cs",
        "Interfaces/ITestSubmissionRepository.cs",
        "Interfaces/IUserTestAttemptRepository.cs",
        "Interfaces/IOrganizationAnalysisService.cs",
        "Interfaces/IReportGenerator.cs",
        // Application DTOs
        "DTOs/ReportDtos.cs",
        // Domain Layer
        "Entities/Assessments",
        "Entities/Tests",
        // Infrastructure
        "Services/Assessments",
        "Services/ReportGenerator.cs",
        "Services/TalentScoreCalculator.cs",
        "Services/OrganizationAnalysisService.cs",
        // Infrastructure Repositories (will be filtered by name patterns)
        "Persistence/Repositories/QuestionRepository.cs",
        "Persistence/Repositories/TalentRepository.cs",
        "Persistence/Repositories/TalentScoreRepository.cs",
        "Persistence/Repositories/TestRepository.cs",
        "Persistence/Repositories/TestResultRepository.cs",
        "Persistence/Repositories/TestSubmissionRepository.cs",
        "Persistence/Repositories/UserTestAttemptRepository.cs",
        "Persistence/Repositories/AssessmentQuestionRepository.cs",
        // Infrastructure Configurations
        "Persistence/Configurations/OrganizationConfiguration.cs",
        "Persistence/Configurations/QuestionConfiguration.cs",
        "Persistence/Configurations/TalentConfiguration.cs",
        "Persistence/Configurations/TalentScoreConfiguration.cs",
        "Persistence/Configurations/TestConfiguration.cs",
        "Persistence/Configurations/TestQuestionConfiguration.cs",
        "Persistence/Configurations/TestQuestionOptionConfiguration.cs",
        "Persistence/Configurations/TestResultConfiguration.cs",
        "Persistence/Configurations/UserTestAnswerConfiguration.cs",
        "Persistence/Configurations/UserTestAttemptConfiguration.cs",
        // WebSite Areas
        "Areas/Organization",
        "Areas/Admin/Controllers/AssessmentController.cs",
        "Areas/Admin/Controllers/TestsController.cs",
        "Areas/Admin/Controllers/OrganizationsController.cs",
        "Areas/Admin/Controllers/ResultController.cs",
        "Areas/Admin/Views/Assessment",
        "Areas/Admin/Views/Tests",
        "Areas/Admin/Views/Organizations",
        "Areas/Admin/Views/Exam",
        "Areas/Admin/Models/Tests",
        "Models/Test",
        "Views/Test",
        "Views/Exam",
        "Controllers/TestController.cs",
        "Controllers/AssessmentController.cs",
        "Controllers/ResultController.cs",
        "Areas/User/Controllers/TestController.cs",
        "Areas/User/Controllers/AssessmentController.cs",
        "Areas/User/Views/Test",
        "Areas/User/Views/Assessment",
        // WebSite Growth
        "Growth",
        // WebSite Models
        "Models/Result",
        // WebSite Views
        "Views/Results",
        "Views/Result",
        // WebSite Routes/Controllers
        "Controllers/MetricsController.cs",
        "Areas/Admin/Controllers/MetricsController.cs",
        // Specific files
        "Question.cs",
        "UserResponse.cs",
        "Talent.cs",
        "TalentScore.cs",
        "Organization.cs",
        "Result.cs"
    };

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico",
        ".woff", ".woff2", ".ttf", ".eot", ".otf",
        ".pdf", ".mp4", ".mp3"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".cshtml", ".csproj", ".razor",
        ".css", ".scss", ".js", ".ts",
        ".json", ".xml", ".resx",
        ".config", ".props", ".targets",
        ".txt", ".md", ".editorconfig",
        ".sln", ".svg"
    };

    public static bool TryClone(ProjectConfig config)
    {
        var websiteSource = ReferencePaths.FindReferenceProjectRoot();
        if (websiteSource is null)
        {
            Console.WriteLine("⚠ Reference web project folder not found. Falling back to template generation.");
            return false;
        }

        var referenceRoot = Directory.GetParent(websiteSource)?.FullName;
        if (referenceRoot is null)
        {
            Console.WriteLine("⚠ Unable to resolve reference root folder.");
            return false;
        }

        var templateKind = DetectTemplateKind(websiteSource);
        var sourceWebProjectName = Path.GetFileName(websiteSource.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        Console.WriteLine($"  Detected source web project: {sourceWebProjectName}");

        // Old layout has src/Domain, src/Application, ... next to the web project.
        // New layout (mainproject) puts Domain/Application/... directly under referenceRoot.
        string referenceSrcRoot;
        var legacySrc = Path.Combine(referenceRoot, "src");
        if (Directory.Exists(legacySrc))
        {
            referenceSrcRoot = legacySrc;
        }
        else
        {
            // Assume new mainproject layout (Domain/Application/Infrastructure/SharedKernel at the root)
            referenceSrcRoot = referenceRoot;
        }

        Console.WriteLine("Cloning reference solution structure...");

        var targetSrcRoot = Path.Combine(config.OutputPath, "src");
        if (Directory.Exists(targetSrcRoot))
        {
            Directory.Delete(targetSrcRoot, recursive: true);
        }
        Directory.CreateDirectory(targetSrcRoot);

        foreach (var folder in SourceDirectories)
        {
            var sourceDir = Path.Combine(referenceSrcRoot, folder);
            if (!Directory.Exists(sourceDir))
            {
                Console.WriteLine($"⚠ Reference directory missing: {folder}");
                continue;
            }

            var targetDir = Path.Combine(targetSrcRoot, folder);
            Console.WriteLine($" - {folder}");
            
            // Check if .csproj file exists in source before copying
            var csprojFile = Path.Combine(sourceDir, $"{folder}.csproj");
            if (File.Exists(csprojFile))
            {
                Console.WriteLine($"    Found .csproj: {folder}.csproj");
            }
            else
            {
                Console.WriteLine($"    ⚠ Missing .csproj: {folder}.csproj");
            }
            
            CopyDirectory(sourceDir, targetDir, config, templateKind, sourceWebProjectName);
            
            // Verify .csproj was copied
            var targetCsproj = Path.Combine(targetDir, $"{folder}.csproj");
            if (File.Exists(targetCsproj))
            {
                Console.WriteLine($"    ✓ Copied .csproj: {folder}.csproj");
            }
            else
            {
                Console.WriteLine($"    ✗ Failed to copy .csproj: {folder}.csproj");
            }
        }

        var websiteTargetDir = Path.Combine(targetSrcRoot, $"{config.ProjectName}.WebSite");
        Console.WriteLine($" - {config.ProjectName}.WebSite (from {sourceWebProjectName})");
        CopyDirectory(websiteSource, websiteTargetDir, config, templateKind, sourceWebProjectName);

        // For mainproject layout, copy any root-level files from referenceSrcRoot to output root
        // (e.g., .editorconfig, .gitignore, README.md, etc.)
        if (!Directory.Exists(legacySrc))
        {
            CopyRootLevelFiles(referenceSrcRoot, config.OutputPath, config, templateKind, sourceWebProjectName);
        }

        // Ensure BaseTypes folder and Result.cs exist in SharedKernel
        EnsureSharedKernelBaseTypes(targetSrcRoot, config);

        Console.WriteLine("✓ Reference source cloned successfully");
        return true;
    }

    private static void EnsureSharedKernelBaseTypes(string srcRoot, ProjectConfig config)
    {
        var sharedKernelPath = Path.Combine(srcRoot, "SharedKernel");
        if (!Directory.Exists(sharedKernelPath))
        {
            Console.WriteLine("⚠ SharedKernel directory not found. Creating it...");
            Directory.CreateDirectory(sharedKernelPath);
        }

        var baseTypesPath = Path.Combine(sharedKernelPath, "BaseTypes");
        if (!Directory.Exists(baseTypesPath))
        {
            Console.WriteLine("Creating SharedKernel/BaseTypes directory...");
            Directory.CreateDirectory(baseTypesPath);
        }

        var resultFilePath = Path.Combine(baseTypesPath, "Result.cs");
        if (!File.Exists(resultFilePath))
        {
            Console.WriteLine("Creating SharedKernel/BaseTypes/Result.cs...");
            var templateProvider = new ProjectGenerator.Core.Templates.TemplateProvider(config.Namespace);
            var resultContent = templateProvider.GetSharedKernelResultTemplate();
            File.WriteAllText(resultFilePath, resultContent, Encoding.UTF8);
        }
    }

    private static void CopyRootLevelFiles(string sourceRoot, string destinationRoot, ProjectConfig config, ReferenceTemplateKind templateKind, string? sourceWebProjectName = null)
    {
        // Copy any files at the root level of mainproject (e.g., .editorconfig, .gitignore, README.md)
        // These should go to the root of the generated project, not into src/
        if (!Directory.Exists(sourceRoot))
        {
            return;
        }

        var rootFiles = Directory.EnumerateFiles(sourceRoot, "*", SearchOption.TopDirectoryOnly)
            .Where(file => !ShouldSkip(file, sourceRoot, templateKind));

        foreach (var file in rootFiles)
        {
            var fileName = Path.GetFileName(file);
            if (string.IsNullOrEmpty(fileName))
            {
                continue;
            }

            // Skip known project folders that are already handled
            var fileNameLower = fileName.ToLowerInvariant();
            if (SourceDirectories.Any(f => f.Equals(fileName, StringComparison.OrdinalIgnoreCase)) ||
                fileName.EndsWith(".WebSite", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var destinationFile = Path.Combine(destinationRoot, fileName);

            if (IsTextFile(file))
            {
                var content = File.ReadAllText(file, Encoding.UTF8);
                var transformedContent = ApplyContentTransformations(content, config, templateKind, fileName, sourceWebProjectName);
                File.WriteAllText(destinationFile, transformedContent, Encoding.UTF8);
                Console.WriteLine($"  Copied root file: {fileName}");
            }
            else
            {
                File.Copy(file, destinationFile, overwrite: true);
                Console.WriteLine($"  Copied root file: {fileName}");
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir, ProjectConfig config, ReferenceTemplateKind templateKind, string? sourceWebProjectName = null)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var directory in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            if (ShouldSkip(directory, sourceDir, templateKind))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(sourceDir, directory);

            // For legacy Arsis template we don't want to carry over old EF Core Migrations,
            // but for the new mainproject template we keep everything as-is.
            if (templateKind == ReferenceTemplateKind.Arsis)
            {
                var normalizedPath = relativePath.Replace('\\', '/');
                var directoryName = Path.GetFileName(normalizedPath);

                if (string.Equals(directoryName, "Migrations", StringComparison.OrdinalIgnoreCase) ||
                    normalizedPath.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) ||
                    normalizedPath.Contains("\\Migrations\\", StringComparison.OrdinalIgnoreCase) ||
                    normalizedPath.Contains("/Migrations", StringComparison.OrdinalIgnoreCase) ||
                    normalizedPath.Contains("\\Migrations", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            var transformedRelative = TransformRelativePath(relativePath, config, sourceWebProjectName);
            Directory.CreateDirectory(Path.Combine(destinationDir, transformedRelative));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var fileName = Path.GetFileName(relativePath);
            
            if (ShouldSkip(file, sourceDir, templateKind))
            {
                // Log skipped .csproj files for debugging
                if (fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"    ⚠ Skipping .csproj file: {relativePath}");
                }
                continue;
            }

            var normalizedPath = relativePath.Replace('\\', '/');

            if (templateKind == ReferenceTemplateKind.Arsis)
            {
                // For the legacy template we drop all migrations so each generated
                // project can create its own clean migration history.
                if (normalizedPath.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) ||
                    normalizedPath.Contains("\\Migrations\\", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip migration snapshot files even if not in Migrations folder
                if (!string.IsNullOrEmpty(fileName) &&
                    fileName.Contains("Snapshot", StringComparison.OrdinalIgnoreCase) &&
                    fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            var transformedRelative = TransformRelativePath(relativePath, config, sourceWebProjectName);
            var destinationFile = Path.Combine(destinationDir, transformedRelative);
            
            // Debug: log .csproj file transformations
            if (fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"    ✓ Copying .csproj: {relativePath} -> {transformedRelative}");
            }
            
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

            if (IsTextFile(file))
            {
                var content = File.ReadAllText(file, Encoding.UTF8);
                var transformedContent = ApplyContentTransformations(content, config, templateKind, fileName, sourceWebProjectName);
                File.WriteAllText(destinationFile, transformedContent, Encoding.UTF8);
            }
            else
            {
                File.Copy(file, destinationFile, overwrite: true);
            }
        }
    }

    private static bool ShouldSkip(string path, string basePath, ReferenceTemplateKind templateKind)
    {
        var relative = Path.GetRelativePath(basePath, path);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        // Skip standard build/version control directories
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            // Skip runtime log folder only when it is at the root of the web project
            // (e.g. Attar.WebSite/logs). We still want to keep code folders like
            // Application/DTOs/Logs.
            if (segment.Equals("logs", StringComparison.OrdinalIgnoreCase))
            {
                // User requirement for mainproject template: copy EVERYTHING exactly as-is,
                // including root-level logs folder inside the web project.
                if (templateKind == ReferenceTemplateKind.Attar)
                {
                    continue;
                }

                // Root-level logs folder (logs/..., not Application/DTOs/Logs)
                if (i == 0)
                {
                    return true;
                }

                // Otherwise, treat as a normal folder (do not skip)
                continue;
            }

            if (SkippedDirectories.Contains(segment))
            {
                // User requirement for mainproject template: do NOT skip bin/obj folders.
                // Keep other safety skips like .git/.vs/TestResults.
                if (templateKind == ReferenceTemplateKind.Attar &&
                    (segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                     segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                return true;
            }
        }

        if (templateKind == ReferenceTemplateKind.Arsis)
        {
            // For the legacy Arsis template we clean up migrations and
            // test/assessment related files. For the new mainproject
            // template we keep everything as-is.

            // Skip all migration files - migrations should be created fresh for each new project
            if (IsMigrationFile(relative))
            {
                return true;
            }

            // Skip test/assessment related paths
            if (IsTestRelatedPath(relative))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMigrationFile(string relativePath)
    {
        // Check if this is a migration file, snapshot file, or Migrations folder
        // Skip all migration-related files and the entire Migrations folder
        var normalizedPath = relativePath.Replace('\\', '/');
        
        // Check if path contains Migrations folder (anywhere in the path)
        if (normalizedPath.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.Contains("\\Migrations\\", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.Contains("/Migrations", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.Contains("\\Migrations", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // Check if file name is a migration snapshot file
        var fileName = Path.GetFileName(normalizedPath);
        if (!string.IsNullOrEmpty(fileName))
        {
            // Migration snapshot files: ApplicationDbContextModelSnapshot.cs or similar
            if (fileName.Contains("Snapshot", StringComparison.OrdinalIgnoreCase) &&
                fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // Migration files usually have pattern like: YYYYMMDDHHMMSS_Description.cs
            if (fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var migrationPattern = @"^\d{14}_";
                if (Regex.IsMatch(fileName, migrationPattern))
                {
                    return true;
                }
            }
            
            // Also check for .Designer.cs files that are part of migrations
            if (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            {
                // Check if parent directory is Migrations
                var parentDir = Path.GetDirectoryName(normalizedPath);
                if (!string.IsNullOrEmpty(parentDir) && 
                    (parentDir.Contains("/Migrations", StringComparison.OrdinalIgnoreCase) ||
                     parentDir.Contains("\\Migrations", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    private static bool IsTestRelatedMigration(string fileName)
    {
        // Check if migration file name contains Test/Assessment/Organization/Talent/Question related keywords
        var testRelatedKeywords = new[]
        {
            "Test",
            "Assessment",
            "Organization",
            "Talent",
            "Question",
            "UserTest",
            "TestAttempt",
            "TestResult",
            "TestQuestion",
            "TestSubmission",
            "AssessmentRun",
            "AssessmentResponse",
            "AssessmentQuestion",
            "TalentScore"
        };

        foreach (var keyword in testRelatedKeywords)
        {
            if (fileName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTestRelatedPath(string relativePath)
    {
        // Normalize path separators
        var normalizedPath = relativePath.Replace('\\', '/');
        
        // Check if any part of the path matches test-related patterns
        foreach (var testPath in TestRelatedPaths)
        {
            var normalizedTestPath = testPath.Replace('\\', '/');
            
            // Check if the path starts with or contains the test-related path
            if (normalizedPath.StartsWith(normalizedTestPath, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Contains($"/{normalizedTestPath}/", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.EndsWith($"/{normalizedTestPath}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // Check for specific file names (improved matching)
            var fileName = Path.GetFileName(normalizedPath);
            var testPathFileName = Path.GetFileName(normalizedTestPath);
            
            // Direct file name match
            if (testPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                fileName != null &&
                fileName.Equals(testPathFileName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // Check if the path ends with the test path (for files in subdirectories)
            if (normalizedPath.EndsWith(normalizedTestPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Additional checks for test-related file names
        var file = Path.GetFileName(relativePath);
        if (file != null)
        {
            // Skip SeedData.cs from Infrastructure.Persistence
            if (file.Equals("SeedData.cs", StringComparison.OrdinalIgnoreCase) &&
                (normalizedPath.Contains("Infrastructure/Persistence", StringComparison.OrdinalIgnoreCase) ||
                 normalizedPath.Contains("Infrastructure\\Persistence", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            
            // Skip Test/Organization related files from Areas/Admin/Models
            if (normalizedPath.Contains("Areas/Admin/Models", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Contains("Areas\\Admin\\Models", StringComparison.OrdinalIgnoreCase))
            {
                var testOrgModelFiles = new[]
                {
                    "Test", "Assessment", "Organization", "Talent", "Question", 
                    "UserTest", "TestAttempt", "TestResult", "TestQuestion",
                    "TestSubmission", "AssessmentRun", "OrganizationStatus"
                };
                
                foreach (var testOrgFile in testOrgModelFiles)
                {
                    if (file.Contains(testOrgFile, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            
            var testRelatedFiles = new[] 
            { 
                "SubmitTestDto.cs", 
                "SubmitTestResultDto.cs", 
                "TalentScoreDto.cs",
                "ReportDocumentDto.cs",
                "ReportDtos.cs",
                "GenerateReportCommand.cs",
                "ReportGenerator.cs",
                "TalentScoreCalculator.cs",
                "OrganizationAnalysisService.cs",
                // Configuration files
                "QuestionConfiguration.cs",
                "TalentConfiguration.cs",
                "TalentScoreConfiguration.cs",
                "TestConfiguration.cs",
                "TestQuestionConfiguration.cs",
                "TestQuestionOptionConfiguration.cs",
                "TestResultConfiguration.cs",
                "UserTestAnswerConfiguration.cs",
                "UserTestAttemptConfiguration.cs",
                // Additional Test/Assessment related files
                "AssessmentLabelResolver.cs",
                "ImportController.cs",
                "ExamController.cs",
                "ResultController.cs",
                "TestListViewModel.cs"
            };
            if (testRelatedFiles.Any(f => file.Equals(f, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Check for Repository files related to Test/Assessment/Talent
            var testRelatedRepositories = new[]
            {
                "QuestionRepository.cs",
                "TalentRepository.cs",
                "TalentScoreRepository.cs",
                "TestRepository.cs",
                "TestResultRepository.cs",
                "TestSubmissionRepository.cs",
                "UserTestAttemptRepository.cs",
                "AssessmentQuestionRepository.cs"
            };
            if (testRelatedRepositories.Any(r => file.Equals(r, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTextFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(extension))
        {
            return true;
        }

        if (BinaryExtensions.Contains(extension))
        {
            return false;
        }

        return TextExtensions.Contains(extension);
    }

    private static ReferenceTemplateKind DetectTemplateKind(string websiteSource)
    {
        var directoryName = Path.GetFileName(websiteSource.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        // Any .WebSite project in mainproject folder is treated as Attar template
        // (supports Attar.WebSite, MobiRooz.WebSite, or any future name)
        if (directoryName.EndsWith(".WebSite", StringComparison.OrdinalIgnoreCase))
        {
            // Check if it's in the mainproject folder
            var parentDir = Directory.GetParent(websiteSource)?.FullName;
            if (parentDir != null)
            {
                var parentName = Path.GetFileName(parentDir);
                if (parentName.Equals("mainproject", StringComparison.OrdinalIgnoreCase))
                {
                    return ReferenceTemplateKind.Attar;
                }
            }
        }

        if (directoryName.Equals("EndPoint.WebSite", StringComparison.OrdinalIgnoreCase))
        {
            return ReferenceTemplateKind.Arsis;
        }

        return ReferenceTemplateKind.Unknown;
    }

    private static string TransformRelativePath(string relativePath, ProjectConfig config, string? sourceWebProjectName = null)
    {
        var transformed = relativePath;

        // Legacy template web project name
        transformed = transformed.Replace("EndPoint.WebSite", $"{config.ProjectName}.WebSite", StringComparison.OrdinalIgnoreCase);

        // New mainproject web project name - replace any .WebSite project name
        if (!string.IsNullOrEmpty(sourceWebProjectName) && sourceWebProjectName.EndsWith(".WebSite", StringComparison.OrdinalIgnoreCase))
        {
            transformed = transformed.Replace(sourceWebProjectName, $"{config.ProjectName}.WebSite", StringComparison.OrdinalIgnoreCase);
            
            // Also replace .csproj and .csproj.user file names
            var sourceBaseName = sourceWebProjectName.Substring(0, sourceWebProjectName.Length - ".WebSite".Length);
            transformed = transformed.Replace($"{sourceWebProjectName}.csproj", $"{config.ProjectName}.WebSite.csproj", StringComparison.OrdinalIgnoreCase);
            transformed = transformed.Replace($"{sourceWebProjectName}.csproj.user", $"{config.ProjectName}.WebSite.csproj.user", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // Fallback: try common names
            transformed = transformed.Replace("Attar.WebSite", $"{config.ProjectName}.WebSite", StringComparison.OrdinalIgnoreCase);
            transformed = transformed.Replace("MobiRooz.WebSite", $"{config.ProjectName}.WebSite", StringComparison.OrdinalIgnoreCase);
            transformed = transformed.Replace("Attar.WebSite.csproj", $"{config.ProjectName}.WebSite.csproj", StringComparison.OrdinalIgnoreCase);
            transformed = transformed.Replace("MobiRooz.WebSite.csproj", $"{config.ProjectName}.WebSite.csproj", StringComparison.OrdinalIgnoreCase);
            transformed = transformed.Replace("Attar.WebSite.csproj.user", $"{config.ProjectName}.WebSite.csproj.user", StringComparison.OrdinalIgnoreCase);
            transformed = transformed.Replace("MobiRooz.WebSite.csproj.user", $"{config.ProjectName}.WebSite.csproj.user", StringComparison.OrdinalIgnoreCase);
        }

        return transformed;
    }

    private static string ApplyContentTransformations(string content, ProjectConfig config, ReferenceTemplateKind templateKind, string? fileName = null, string? sourceWebProjectName = null)
    {
        return templateKind switch
        {
            ReferenceTemplateKind.Attar => ApplyAttarTransformations(content, config, fileName, sourceWebProjectName),
            ReferenceTemplateKind.Arsis or ReferenceTemplateKind.Unknown => ApplyArsisTransformations(content, config, fileName),
            _ => ApplyArsisTransformations(content, config, fileName)
        };
    }

    private static string ApplyAttarTransformations(string content, ProjectConfig config, string? fileName, string? sourceWebProjectName = null)
    {
        // Extract the base name from source web project (e.g., "MobiRooz" from "MobiRooz.WebSite")
        string? sourceBaseName = null;
        if (!string.IsNullOrEmpty(sourceWebProjectName) && sourceWebProjectName.EndsWith(".WebSite", StringComparison.OrdinalIgnoreCase))
        {
            sourceBaseName = sourceWebProjectName.Substring(0, sourceWebProjectName.Length - ".WebSite".Length);
        }

        // For the new mainproject template we only do safe renames (namespaces,
        // project name, and database names). We do NOT strip any features –
        // the generated project should be structurally identical to the
        // original template project, just under a new name/namespace.
        var transformed = content;

        // Replace web project name (dynamic based on source)
        if (!string.IsNullOrEmpty(sourceBaseName))
        {
            transformed = transformed.Replace($"{sourceBaseName}.WebSite", $"{config.ProjectName}.WebSite", StringComparison.Ordinal);
        }
        // Fallback to common names
        transformed = transformed
            .Replace("Attar.WebSite", $"{config.ProjectName}.WebSite", StringComparison.Ordinal)
            .Replace("MobiRooz.WebSite", $"{config.ProjectName}.WebSite", StringComparison.Ordinal);

        // Replace root namespace (dynamic based on source)
        if (!string.IsNullOrEmpty(sourceBaseName))
        {
            transformed = transformed
                .Replace($"namespace {sourceBaseName}", $"namespace {config.Namespace}", StringComparison.Ordinal)
                .Replace($"{sourceBaseName}.", $"{config.Namespace}.", StringComparison.Ordinal);
        }
        // Fallback to common names
        transformed = transformed
            .Replace("namespace Attar", $"namespace {config.Namespace}", StringComparison.Ordinal)
            .Replace("Attar.", $"{config.Namespace}.", StringComparison.Ordinal)
            .Replace("namespace MobiRooz", $"namespace {config.Namespace}", StringComparison.Ordinal)
            .Replace("MobiRooz.", $"{config.Namespace}.", StringComparison.Ordinal);

        // Replace database names (dynamic based on source)
        if (!string.IsNullOrEmpty(sourceBaseName))
        {
            transformed = transformed
                .Replace($"{sourceBaseName}_DB", $"{config.ProjectName}_DB", StringComparison.Ordinal)
                .Replace($"{sourceBaseName}_Logs_DB", $"{config.ProjectName}_Logs_DB", StringComparison.Ordinal)
                .Replace($"{sourceBaseName}_Hangfire_DB", $"{config.ProjectName}_Hangfire_DB", StringComparison.Ordinal);
        }
        // Fallback to common names
        transformed = transformed
            .Replace("Attar_DB", $"{config.ProjectName}_DB", StringComparison.Ordinal)
            .Replace("Attar_Logs_DB", $"{config.ProjectName}_Logs_DB", StringComparison.Ordinal)
            .Replace("Attar_Hangfire_DB", $"{config.ProjectName}_Hangfire_DB", StringComparison.Ordinal)
            .Replace("MobiRooz_DB", $"{config.ProjectName}_DB", StringComparison.Ordinal)
            .Replace("MobiRooz_Logs_DB", $"{config.ProjectName}_Logs_DB", StringComparison.Ordinal)
            .Replace("MobiRooz_Hangfire_DB", $"{config.ProjectName}_Hangfire_DB", StringComparison.Ordinal);

        // ApplicationName in appsettings.json (should be from database, but we set a default)
        if (!string.IsNullOrEmpty(sourceBaseName))
        {
            transformed = transformed
                .Replace($"\"{sourceBaseName}-WebSite\"", $"\"{config.ProjectName}-WebSite\"", StringComparison.Ordinal)
                .Replace($"\"ApplicationName\": \"{sourceBaseName}-WebSite\"", $"\"ApplicationName\": \"{config.ProjectName}-WebSite\"", StringComparison.Ordinal);
        }
        // Fallback to common names
        transformed = transformed
            .Replace("Attar-WebSite", $"{config.ProjectName}-WebSite", StringComparison.Ordinal)
            .Replace("MobiRooz-WebSite", $"{config.ProjectName}-WebSite", StringComparison.Ordinal);

        // Replace hardcoded default values in Program.cs
        if (!string.IsNullOrEmpty(sourceBaseName))
        {
            transformed = transformed
                .Replace($"?? \"{sourceBaseName}\"", $"?? \"{config.ProjectName}\"", StringComparison.Ordinal)
                .Replace($"?? \"{sourceBaseName}-WebSite\"", $"?? \"{config.ProjectName}-WebSite\"", StringComparison.Ordinal);
        }
        // Fallback to common names
        transformed = transformed
            .Replace("?? \"Attar\"", $"?? \"{config.ProjectName}\"", StringComparison.Ordinal)
            .Replace("?? \"MobiRooz\"", $"?? \"{config.ProjectName}\"", StringComparison.Ordinal);

        // Normalize logging entity and table names so they are not tied to the
        // original project name. We intentionally use a neutral name
        // "ApplicationLog(s)" so the same template can be reused for any project.
        transformed = transformed
            // SQL / configuration table names and indexes
            .Replace("[Logs].[AttarApplicationLogs]", "[Logs].[ApplicationLogs]", StringComparison.Ordinal)
            .Replace("IX_AttarApplicationLogs_", "IX_ApplicationLogs_", StringComparison.Ordinal)
            // EF Core configuration
            .Replace("ToTable(\"AttarApplicationLogs\"", "ToTable(\"ApplicationLogs\"", StringComparison.Ordinal)
            // DbSet and entity class names
            .Replace("AttarApplicationLogs", "ApplicationLogs", StringComparison.Ordinal)
            .Replace("AttarApplicationLog", "ApplicationLog", StringComparison.Ordinal);

        // Fix project references in .csproj files to ensure correct relative paths
        // In mainproject template, all projects are in src/ folder, so paths like "../Domain/Domain.csproj" are correct
        // But we need to ensure they point to the right location
        if (fileName != null && fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            // Ensure ProjectReference paths are relative and correct
            // Pattern: <ProjectReference Include="../SomeProject/SomeProject.csproj" />
            // This should already be correct for mainproject template, but we verify
            transformed = Regex.Replace(
                transformed,
                @"(<ProjectReference\s+Include=[""'])(\.\./)([^""']+\.csproj)([""']\s*/>)",
                match =>
                {
                    var projectName = Path.GetFileNameWithoutExtension(match.Groups[3].Value);
                    // Ensure the path points to the correct project in src/
                    return $"{match.Groups[1].Value}../{projectName}/{projectName}.csproj{match.Groups[4].Value}";
                },
                RegexOptions.IgnoreCase
            );
        }

        return transformed;
    }

    private static string ApplyArsisTransformations(string content, ProjectConfig config, string? fileName)
    {
        var transformed = content
            .Replace("EndPoint.WebSite", $"{config.ProjectName}.WebSite", StringComparison.Ordinal)
            // IMPORTANT: Replace ArsisTest_DB FIRST before replacing ArsisTest
            // This ensures connection strings get the correct database name
            .Replace("ArsisTest_DB", $"{config.ProjectName}_DB", StringComparison.Ordinal)
            // Replace namespace Arsis with new namespace
            .Replace("namespace Arsis", $"namespace {config.Namespace}", StringComparison.Ordinal)
            // Replace Arsis. with new namespace prefix
            .Replace("Arsis.", $"{config.Namespace}.", StringComparison.Ordinal)
            // Replace ArsisTest with new namespace (for any remaining references)
            .Replace("ArsisTest", config.Namespace, StringComparison.Ordinal);

        // Fix project references: remove "../src/" from paths since all projects are now in the same src folder
        // Pattern: <ProjectReference Include="../src/SomeProject/SomeProject.csproj" />
        // Should become: <ProjectReference Include="../SomeProject/SomeProject.csproj" />
        transformed = Regex.Replace(
            transformed,
            @"(<ProjectReference\s+Include=[""'])(\.\./src/)([^""']+\.csproj)([""']\s*/>)",
            "$1../$3$4",
            RegexOptions.IgnoreCase
        );

        // Remove Assessment/Test related code from Program.cs
        if (fileName != null && fileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase))
        {
            transformed = RemoveAssessmentCode(transformed);
            transformed = RemoveMetricsRoute(transformed);
        }

        // Remove Assessment/Test menu items from AdminSidebar
        if (fileName != null && fileName.Equals("Default.cshtml", StringComparison.OrdinalIgnoreCase))
        {
            transformed = RemoveTestMenuItems(transformed);
        }

        // Remove Test/Report service registrations from DependencyInjection
        if (fileName != null && fileName.Equals("DependencyInjection.cs", StringComparison.OrdinalIgnoreCase))
        {
            transformed = RemoveTestServiceRegistrations(transformed);
        }

        // Remove BuildLearningMetricsAsync method from AdminDashboardMetricsService
        if (fileName != null && fileName.Equals("AdminDashboardMetricsService.cs", StringComparison.OrdinalIgnoreCase))
        {
            transformed = RemoveBuildLearningMetricsAsync(transformed);
        }

        // Remove LearningMetricsDto from SystemPerformanceSummaryDto
        if (fileName != null && fileName.Equals("SystemPerformanceSummaryDto.cs", StringComparison.OrdinalIgnoreCase))
        {
            transformed = RemoveLearningMetricsFromSystemPerformanceSummaryDto(transformed);
        }

        // Remove Test-related interface references from C# files
        if (fileName != null && fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            transformed = RemoveTestInterfaceReferences(transformed);
            // Also remove Test/Assessment related using statements and references
            transformed = RemoveTestRelatedReferences(transformed);
            
            // Special handling for BaseEntity.cs to remove ForeignKey attributes for non-existent properties
            if (fileName != null && fileName.Equals("BaseEntity.cs", StringComparison.OrdinalIgnoreCase))
            {
                transformed = RemoveForeignKeyAttributesFromBaseEntity(transformed);
            }
            
            // Special handling for DiscountCode.cs to remove DiscountApplicationResult references
            if (fileName != null && fileName.Equals("DiscountCode.cs", StringComparison.OrdinalIgnoreCase))
            {
                transformed = RemoveDiscountApplicationResultReferences(transformed);
            }
            
            // Special handling for ICommand.cs to fix BaseTypes and Result references
            if (fileName != null && fileName.Equals("ICommand.cs", StringComparison.OrdinalIgnoreCase))
            {
                transformed = FixICommandReferences(transformed);
            }
            
            // Special handling for DiscountDtos.cs to add using statement and fix references
            if (fileName != null && fileName.Equals("DiscountDtos.cs", StringComparison.OrdinalIgnoreCase))
            {
                transformed = AddDiscountDtosUsingStatement(transformed);
                // Also fix DiscountApplicationResult references to use DiscountCode.DiscountApplicationResult
                transformed = FixDiscountApplicationResultReferences(transformed);
            }
            
            // Remove non-existent properties from entity classes (that don't exist in database)
            // Check if this is an entity class (in Domain/Entities or Domain/Base folder)
            if (content.Contains("namespace", StringComparison.OrdinalIgnoreCase) &&
                (content.Contains("Domain.Entities", StringComparison.OrdinalIgnoreCase) ||
                 content.Contains("Domain\\Entities", StringComparison.OrdinalIgnoreCase) ||
                 content.Contains("Domain.Base", StringComparison.OrdinalIgnoreCase) ||
                 content.Contains("Domain\\Base", StringComparison.OrdinalIgnoreCase)))
            {
                transformed = RemoveNonExistentPropertiesFromEntity(transformed);
            }
            
            // Special handling for DashboardController and SystemPerformanceSummaryViewModel
            if (fileName.Equals("DashboardController.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("SystemPerformanceSummaryViewModel.cs", StringComparison.OrdinalIgnoreCase))
            {
                transformed = RemoveLearningMetricsReferences(transformed);
                transformed = FixDashboardControllerBuildEmptySummary(transformed);
            }
            
            // Special handling for CheckoutController
            if (fileName.Equals("CheckoutController.cs", StringComparison.OrdinalIgnoreCase))
            {
                transformed = RemoveTestResultReferences(transformed);
                transformed = RemoveCheckoutControllerTestMethods(transformed);
            }
        }
        
        // Special handling for all Details.cshtml views (Invoice, Catalog, etc.)
        if (fileName != null && fileName.Equals("Details.cshtml", StringComparison.OrdinalIgnoreCase))
        {
            transformed = RemoveInvoiceTestProperties(transformed);
            transformed = CleanInvoiceDetailsView(transformed);
            // Also add using statements for all Details views
            transformed = AddUsingStatementsToDetailsView(transformed);
        }
        
        // Special handling for _ViewImports.cshtml to remove WebSite.Models.Result reference
        if (fileName != null && fileName.Equals("_ViewImports.cshtml", StringComparison.OrdinalIgnoreCase))
        {
            transformed = RemoveWebSiteModelsResultReference(transformed);
        }

        // Special handling for AppDbContext to remove Organization references
        if (fileName != null && fileName.Equals("AppDbContext.cs", StringComparison.OrdinalIgnoreCase))
        {
            transformed = RemoveOrganizationFromDbContext(transformed);
        }

        // Ignore properties that don't exist in database for entity configurations
        if (fileName != null && fileName.EndsWith("Configuration.cs", StringComparison.OrdinalIgnoreCase))
        {
            transformed = IgnoreNonExistentPropertiesInConfiguration(transformed);
        }

        // Clean migration files to remove Test/Assessment/Organization related code
        if (fileName != null && fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            // Check if this is a migration file
            var relativePath = content.Contains("namespace", StringComparison.OrdinalIgnoreCase) 
                ? string.Empty 
                : string.Empty; // We'll check by file path in ShouldSkip, but also clean content if needed
            
            // If file contains migration-related code and Test/Assessment references, clean it
            if (content.Contains("migrationBuilder", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("CreateTable", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("DropTable", StringComparison.OrdinalIgnoreCase))
            {
                transformed = CleanMigrationFile(transformed);
            }
        }

        // SeedData.cs is now skipped entirely, so no special handling needed

        return transformed;
    }

    private static string RemoveTestEntitiesFromSeedData(string content)
    {
        // Strategy: First remove Talent/Question related code, then format SystemUser block correctly
        
        // First, change new() to new ApplicationUser if needed (before we process the block)
        var systemUserInitPattern = @"(internal\s+static\s+readonly\s+ApplicationUser\s+SystemUser\s*=\s*)new\(\)";
        content = Regex.Replace(
            content,
            systemUserInitPattern,
            "$1new ApplicationUser",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Find and format SystemUser block FIRST (before removing other code)
        var systemUserStartPattern = @"internal\s+static\s+readonly\s+ApplicationUser\s+SystemUser\s*=\s*new\s+ApplicationUser";
        var systemUserStartMatch = Regex.Match(content, systemUserStartPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        int systemUserEnd = 0;
        if (systemUserStartMatch.Success)
        {
            var startIndex = systemUserStartMatch.Index;
            var declarationEnd = startIndex + systemUserStartMatch.Length;
            
            // Find the opening brace
            var braceStart = content.IndexOf('{', declarationEnd);
            if (braceStart >= 0)
            {
                // Use brace matching to find the closing brace
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                
                if (braceCount == 0)
                {
                    // Find semicolon after closing brace
                    var semicolonIndex = i;
                    while (semicolonIndex < content.Length && char.IsWhiteSpace(content[semicolonIndex]))
                        semicolonIndex++;
                    if (semicolonIndex < content.Length && content[semicolonIndex] == ';')
                        semicolonIndex++;
                    
                    // Extract properties block
                    var propertiesContent = content.Substring(braceStart + 1, i - 1 - braceStart - 1);
                    
                    // Parse properties line by line and ensure ALL have commas
                    var lines = propertiesContent.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                    var processedLines = new List<string>();
                    
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        
                        // Skip empty lines
                        if (string.IsNullOrWhiteSpace(trimmed))
                            continue;
                        
                        // Skip braces if any
                        if (trimmed == "{" || trimmed == "}")
                            continue;
                        
                        // Check if it's a property assignment (contains =)
                        if (trimmed.Contains("="))
                        {
                            // Remove any existing comma or semicolon
                            trimmed = trimmed.TrimEnd(',', ';').TrimEnd();
                            
                            // Always add comma at the end
                            processedLines.Add("    " + trimmed + ",");
                        }
                    }

                    // Build the formatted SystemUser block with proper closing
                    var formattedBlock = "internal static readonly ApplicationUser SystemUser = new ApplicationUser\r\n" +
                                       "{\r\n" +
                                       string.Join("\r\n", processedLines) + "\r\n" +
                                       "};";
                    
                    // Replace the old block with the formatted one
                    var endIndex = semicolonIndex;
                    while (endIndex < content.Length && (content[endIndex] == '\r' || content[endIndex] == '\n'))
                        endIndex++;
                    
                    content = content.Substring(0, startIndex) + 
                             formattedBlock + 
                             content.Substring(endIndex);
                    
                    // Update systemUserEnd to the new position after replacement
                    systemUserEnd = startIndex + formattedBlock.Length;
                }
            }
        }
        
        // Now remove everything from after SystemUser to before the class closing brace
        if (systemUserEnd > 0)
        {
            // Find the class closing brace (the last } before the end of file or namespace)
            var classEndIndex = content.LastIndexOf('}');
            
            if (classEndIndex > systemUserEnd)
            {
                // Find the start of the line containing the closing brace
                var braceLineStart = classEndIndex;
                while (braceLineStart > 0 && content[braceLineStart - 1] != '\n' && content[braceLineStart - 1] != '\r')
                    braceLineStart--;
                
                // Include the newline before the closing brace line if it exists
                if (braceLineStart > 0 && (content[braceLineStart - 1] == '\n' || content[braceLineStart - 1] == '\r'))
                {
                    braceLineStart--;
                    if (braceLineStart > 0 && content[braceLineStart - 1] == '\r' && content[braceLineStart] == '\n')
                        braceLineStart--;
                }
                
                // Remove everything from after SystemUser to before the class closing brace
                content = content.Substring(0, systemUserEnd) + 
                         (braceLineStart > systemUserEnd ? content.Substring(braceLineStart) : content.Substring(classEndIndex));
            }
        }
        
        // After removing Talent/Question code, reformat SystemUser block again to ensure commas are correct
        // This is a safety measure in case the removal process affected the block
        // Use a more flexible pattern to find SystemUser block
        var systemUserFlexiblePattern = @"internal\s+static\s+readonly\s+ApplicationUser\s+SystemUser\s*=\s*new\s+ApplicationUser";
        var finalSystemUserMatch = Regex.Match(content, systemUserFlexiblePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        if (finalSystemUserMatch.Success)
        {
            var startIndex = finalSystemUserMatch.Index;
            var declarationEnd = startIndex + finalSystemUserMatch.Length;
            
            // Find the opening brace
            var braceStart = content.IndexOf('{', declarationEnd);
            if (braceStart >= 0)
            {
                // Use brace matching to find the closing brace
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                
                if (braceCount == 0)
                {
                    // Find semicolon after closing brace
                    var semicolonIndex = i;
                    while (semicolonIndex < content.Length && char.IsWhiteSpace(content[semicolonIndex]))
                        semicolonIndex++;
                    if (semicolonIndex < content.Length && content[semicolonIndex] == ';')
                        semicolonIndex++;
                    
                    // Extract properties block
                    var propertiesContent = content.Substring(braceStart + 1, i - 1 - braceStart - 1);
                    
                    // Parse properties line by line and ensure ALL have commas
                    var lines = propertiesContent.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                    var processedLines = new List<string>();
                    
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        
                        // Skip empty lines
                        if (string.IsNullOrWhiteSpace(trimmed))
                            continue;
                        
                        // Skip braces if any
                        if (trimmed == "{" || trimmed == "}")
                            continue;
                        
                        // Check if it's a property assignment (contains =)
                        if (trimmed.Contains("="))
                        {
                            // Remove any existing comma or semicolon
                            trimmed = trimmed.TrimEnd(',', ';').TrimEnd();
                            
                            // Always add comma at the end
                            processedLines.Add("    " + trimmed + ",");
                        }
                    }
                    
                    // Build the formatted SystemUser block with proper closing
                    var formattedBlock = "internal static readonly ApplicationUser SystemUser = new ApplicationUser\r\n" +
                                       "{\r\n" +
                                       string.Join("\r\n", processedLines) + "\r\n" +
                                       "};";
                    
                    // Replace the old block with the formatted one
                    var endIndex = semicolonIndex;
                    while (endIndex < content.Length && (content[endIndex] == '\r' || content[endIndex] == '\n'))
                        endIndex++;
                    
                    content = content.Substring(0, startIndex) + 
                             formattedBlock + 
                             content.Substring(endIndex);
                }
            }
        }
        
        // Clean up any remaining orphaned elements as a safety measure
        // Remove any remaining Talent ID constants
        var talentIdNames = new[]
        {
            "AnalyticalThinkingId", "CreativeVisionId", "LeadershipInfluenceId", "TeamCollaborationId",
            "EmotionalIntelligenceId", "StrategicPlanningId", "ProblemSolvingId", "AdaptabilityId",
            "CommunicationExcellenceId", "DecisionMakingId", "InnovationCatalystId", "ResilienceId",
            "LearningAgilityId", "TimeManagementId", "CustomerFocusId", "TechnicalMasteryId",
            "MentoringId", "NegotiationId", "ConflictResolutionId", "EntrepreneurshipId",
            "AttentionToDetailId", "ProductivityDriveId", "CulturalAwarenessId", "ServiceOrientationId"
        };

        foreach (var idName in talentIdNames)
        {
            content = Regex.Replace(
                content,
                $@"^\s*internal\s+static\s+readonly\s+Guid\s+{Regex.Escape(idName)}\s*=\s*Guid\.Parse\([^)]+\);\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }
        
        // FINAL STEP: Reformat SystemUser block one more time after all removals
        // This ensures the block is correctly formatted with commas and proper closing
        var finalSystemUserPattern = @"internal\s+static\s+readonly\s+ApplicationUser\s+SystemUser\s*=\s*new\s+ApplicationUser";
        var finalMatch = Regex.Match(content, finalSystemUserPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        if (finalMatch.Success)
        {
            var startIndex = finalMatch.Index;
            var declarationEnd = startIndex + finalMatch.Length;
            
            var braceStart = content.IndexOf('{', declarationEnd);
            if (braceStart >= 0)
            {
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                
                if (braceCount == 0)
                {
                    var semicolonIndex = i;
                    while (semicolonIndex < content.Length && char.IsWhiteSpace(content[semicolonIndex]))
                        semicolonIndex++;
                    if (semicolonIndex < content.Length && content[semicolonIndex] == ';')
                        semicolonIndex++;
                    
                    var propertiesContent = content.Substring(braceStart + 1, i - 1 - braceStart - 1);
                    var lines = propertiesContent.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                    var processedLines = new List<string>();
                    
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed))
                            continue;
                        if (trimmed == "{" || trimmed == "}")
                            continue;
                        if (trimmed.Contains("="))
                        {
                            trimmed = trimmed.TrimEnd(',', ';').TrimEnd();
                            processedLines.Add("    " + trimmed + ",");
                        }
                    }
                    
                    var formattedBlock = "internal static readonly ApplicationUser SystemUser = new ApplicationUser\r\n" +
                                       "{\r\n" +
                                       string.Join("\r\n", processedLines) + "\r\n" +
                                       "};";
                    
                    var endIndex = semicolonIndex;
                    while (endIndex < content.Length && (content[endIndex] == '\r' || content[endIndex] == '\n'))
                        endIndex++;
                    
                    content = content.Substring(0, startIndex) + 
                             formattedBlock + 
                             content.Substring(endIndex);
                }
            }
        }

        // Remove Talents collection - use a more robust approach
        // Match from "internal static readonly IReadOnlyCollection<object> Talents" to the closing "};"
        var talentsPattern = @"internal\s+static\s+readonly\s+IReadOnlyCollection<object>\s+Talents\s*=\s*new\s*\[";
        var talentsMatch = Regex.Match(content, talentsPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        while (talentsMatch.Success)
        {
            var startIndex = talentsMatch.Index;
            // Find the start of the line
            var lineStart = startIndex;
            while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                lineStart--;
            
            var bracketStart = content.IndexOf('[', startIndex);
            if (bracketStart >= 0)
            {
                int bracketCount = 1;
                int i = bracketStart + 1;
                while (i < content.Length && bracketCount > 0)
                {
                    if (content[i] == '[') bracketCount++;
                    else if (content[i] == ']') bracketCount--;
                    i++;
                }
                if (bracketCount == 0)
                {
                    // Find semicolon after closing bracket
                    while (i < content.Length && char.IsWhiteSpace(content[i]))
                        i++;
                    if (i < content.Length && content[i] == ';')
                        i++;
                    // Include newline(s) after semicolon
                    while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                        i++;
                    // Remove from line start to end of declaration
                    content = content.Remove(lineStart, i - lineStart);
                    // Try to find next match
                    talentsMatch = Regex.Match(content, talentsPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        // Remove Questions collection - use a more robust approach
        var questionsPattern = @"internal\s+static\s+readonly\s+IReadOnlyCollection<object>\s+Questions\s*=\s*new\s*\[";
        var questionsMatch = Regex.Match(content, questionsPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        while (questionsMatch.Success)
        {
            var startIndex = questionsMatch.Index;
            // Find the start of the line
            var lineStart = startIndex;
            while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                lineStart--;
            
            var bracketStart = content.IndexOf('[', startIndex);
            if (bracketStart >= 0)
            {
                int bracketCount = 1;
                int i = bracketStart + 1;
                while (i < content.Length && bracketCount > 0)
                {
                    if (content[i] == '[') bracketCount++;
                    else if (content[i] == ']') bracketCount--;
                    i++;
                }
                if (bracketCount == 0)
                {
                    // Find semicolon after closing bracket
                    while (i < content.Length && char.IsWhiteSpace(content[i]))
                        i++;
                    if (i < content.Length && content[i] == ';')
                        i++;
                    // Include newline(s) after semicolon
                    while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                        i++;
                    // Remove from line start to end of declaration
                    content = content.Remove(lineStart, i - lineStart);
                    // Try to find next match
                    questionsMatch = Regex.Match(content, questionsPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        // Remove CreateTalent method - improved pattern to handle expression body with nested braces
        var createTalentPattern = @"private\s+static\s+object\s+CreateTalent\([^)]+\)\s*=>\s*new\s*\{";
        var createTalentMatch = Regex.Match(content, createTalentPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (createTalentMatch.Success)
        {
            var startIndex = createTalentMatch.Index;
            var braceStart = content.IndexOf('{', startIndex);
            if (braceStart >= 0)
            {
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                if (braceCount == 0)
                {
                    // Find semicolon after closing brace
                    while (i < content.Length && char.IsWhiteSpace(content[i]))
                        i++;
                    if (i < content.Length && content[i] == ';')
                        i++;
                    // Include newline
                    while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                        i++;
                    content = content.Remove(startIndex, i - startIndex);
                }
            }
        }

        // Remove CreateQuestion method - same approach
        var createQuestionPattern = @"private\s+static\s+object\s+CreateQuestion\([^)]+\)\s*=>\s*new\s*\{";
        var createQuestionMatch = Regex.Match(content, createQuestionPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (createQuestionMatch.Success)
        {
            var startIndex = createQuestionMatch.Index;
            var braceStart = content.IndexOf('{', startIndex);
            if (braceStart >= 0)
            {
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                if (braceCount == 0)
                {
                    // Find semicolon after closing brace
                    while (i < content.Length && char.IsWhiteSpace(content[i]))
                        i++;
                    if (i < content.Length && content[i] == ';')
                        i++;
                    // Include newline
                    while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                        i++;
                    content = content.Remove(startIndex, i - startIndex);
                }
            }
        }

        // Remove all CreateQuestion calls - use a more aggressive approach
        // Match CreateQuestion( and remove everything until the closing ) and optional comma
        var createQuestionCallPattern = @"CreateQuestion\s*\(";
        var questionMatches = Regex.Matches(content, createQuestionCallPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        // Process in reverse order to maintain indices
        for (int matchIndex = questionMatches.Count - 1; matchIndex >= 0; matchIndex--)
        {
            var match = questionMatches[matchIndex];
            var startIndex = match.Index;
            // Find the start of the line
            var lineStart = startIndex;
            while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                lineStart--;
            
            var parenStart = content.IndexOf('(', startIndex);
            if (parenStart >= 0)
            {
                int parenCount = 1;
                int i = parenStart + 1;
                bool inString = false;
                char stringChar = '\0';
                
                while (i < content.Length && parenCount > 0)
                {
                    if (!inString)
                    {
                        if (content[i] == '"' || content[i] == '\'')
                        {
                            inString = true;
                            stringChar = content[i];
                        }
                        else if (content[i] == '(') parenCount++;
                        else if (content[i] == ')') parenCount--;
                    }
                    else
                    {
                        if (content[i] == stringChar && (i == 0 || content[i - 1] != '\\'))
                            inString = false;
                    }
                    i++;
                }
                
                if (parenCount == 0)
                {
                    // Find comma after closing parenthesis (for array elements)
                    while (i < content.Length && char.IsWhiteSpace(content[i]))
                        i++;
                    if (i < content.Length && content[i] == ',')
                        i++;
                    // Include newline(s) after comma
                    while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                        i++;
                    
                    // Remove from line start to end of call
                    content = content.Remove(lineStart, i - lineStart);
                }
            }
        }

        // Clean up any remaining CreateTalent calls
        var createTalentCallPattern = @"CreateTalent\s*\(";
        var talentMatches = Regex.Matches(content, createTalentCallPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        for (int matchIndex = talentMatches.Count - 1; matchIndex >= 0; matchIndex--)
        {
            var match = talentMatches[matchIndex];
            var startIndex = match.Index;
            var parenStart = content.IndexOf('(', startIndex);
            if (parenStart >= 0)
            {
                int parenCount = 1;
                int i = parenStart + 1;
                bool inString = false;
                char stringChar = '\0';
                
                while (i < content.Length && parenCount > 0)
                {
                    if (!inString)
                    {
                        if (content[i] == '"' || content[i] == '\'')
                        {
                            inString = true;
                            stringChar = content[i];
                        }
                        else if (content[i] == '(') parenCount++;
                        else if (content[i] == ')') parenCount--;
                    }
                    else
                    {
                        if (content[i] == stringChar && (i == 0 || content[i - 1] != '\\'))
                            inString = false;
                    }
                    i++;
                }
                
                if (parenCount == 0)
                {
                    // Find semicolon
                    while (i < content.Length && char.IsWhiteSpace(content[i]))
                        i++;
                    if (i < content.Length && content[i] == ';')
                        i++;
                    // Include newline
                    while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                        i++;
                    
                    // Remove the entire line including leading whitespace
                    var lineStart = startIndex;
                    while (lineStart > 0 && char.IsWhiteSpace(content[lineStart - 1]) && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                        lineStart--;
                    
                    content = content.Remove(lineStart, i - lineStart);
                }
            }
        }

        // Remove any remaining references to Talent IDs (standalone or in parameter lists)
        foreach (var idName in talentIdNames)
        {
            // Remove standalone Talent ID references on their own line
            content = Regex.Replace(
                content,
                $@"^\s*{Regex.Escape(idName)}\s*,?\s*$\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            
            // Remove Talent ID references followed by comma and newline
            content = Regex.Replace(
                content,
                $@"\s*{Regex.Escape(idName)}\s*,?\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }

        // Remove any orphaned Guid.Parse calls that might be left
        content = Regex.Replace(
            content,
            @"Guid\.Parse\([^)]+\)\s*,?\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        // Remove any lines that are just commas or closing parentheses (orphaned from removed array elements)
        content = Regex.Replace(
            content,
            @"^\s*[,)]\s*$\r?\n",
            string.Empty,
            RegexOptions.Multiline
        );

        // Remove orphaned commas at the end of lines (leftover from removed array elements)
        content = Regex.Replace(
            content,
            @",\s*$\r?\n",
            "\r\n",
            RegexOptions.Multiline
        );

        // Remove orphaned closing parentheses followed by comma (leftover from removed calls)
        content = Regex.Replace(
            content,
            @"\)\s*,\s*$\r?\n",
            "\r\n",
            RegexOptions.Multiline
        );

        // Remove any remaining orphaned syntax elements
        // Remove lines that start with comma or closing parenthesis
        content = Regex.Replace(
            content,
            @"^\s*[,)]\s*$\r?\n",
            string.Empty,
            RegexOptions.Multiline
        );

        // Remove trailing commas before newlines (orphaned from removed array elements)
        content = Regex.Replace(
            content,
            @",\s*(\r?\n)",
            "$1",
            RegexOptions.Multiline
        );

        // Remove orphaned closing parentheses at end of lines
        content = Regex.Replace(
            content,
            @"\)\s*(\r?\n)",
            "$1",
            RegexOptions.Multiline
        );

        // Remove any remaining CreateQuestion or CreateTalent references
        content = Regex.Replace(
            content,
            @"\s*Create(Question|Talent)\s*\([^)]*\)\s*,?\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );

        // Clean up multiple consecutive empty lines
        content = Regex.Replace(
            content,
            @"(\r?\n\s*){3,}",
            "\r\n\r\n",
            RegexOptions.Multiline
        );

        // Remove only truly orphaned closing brackets/braces that are clearly leftover from removed collections
        // Pattern: lines that are just "};" or "];" (orphaned from removed array declarations)
        content = Regex.Replace(
            content,
            @"^\s*[}\]]\s*;\s*$\r?\n",
            string.Empty,
            RegexOptions.Multiline
        );

        // Remove orphaned opening brackets/braces on their own lines (leftover from removed collections)
        // But only if they're not part of a valid declaration
        content = Regex.Replace(
            content,
            @"^\s*[{\[]\s*$\r?\n(?=\s*[}\]]\s*;?\s*$)",
            string.Empty,
            RegexOptions.Multiline
        );

        // Remove trailing whitespace and excessive empty lines before the closing brace of the class
        // Find the last closing brace of the class
        var lastBraceIndex = content.LastIndexOf('}');
        if (lastBraceIndex > 0)
        {
            // Get content before the last brace
            var beforeLastBrace = content.Substring(0, lastBraceIndex);
            // Remove excessive trailing whitespace and empty lines (keep max 2 empty lines)
            beforeLastBrace = Regex.Replace(
                beforeLastBrace,
                @"(\r?\n\s*){4,}$",
                "\r\n\r\n",
                RegexOptions.Multiline
            );
            content = beforeLastBrace + content.Substring(lastBraceIndex);
        }

        // Final cleanup: remove only orphaned closing brackets/braces that are clearly syntax errors
        // Pattern: "};" or "];" on their own line (orphaned from removed array/method declarations)
        content = Regex.Replace(
            content,
            @"^\s*[}\]]\s*;\s*$\r?\n",
            string.Empty,
            RegexOptions.Multiline
        );

        // Clean up multiple consecutive empty lines (max 2)
        content = Regex.Replace(
            content,
            @"(\r?\n\s*){3,}",
            "\r\n\r\n",
            RegexOptions.Multiline
        );

        return content;
    }

    private static string RemoveOrganizationFromDbContext(string content)
    {
        // Remove all Test/Assessment/Organization related DbSet declarations
        var testEntityTypes = new[]
        {
            "Talent",
            "Question",
            "UserResponse",
            "TalentScore",
            "Test",
            "TestQuestion",
            "TestQuestionOption",
            "UserTestAttempt",
            "UserTestAnswer",
            "TestResult",
            "AssessmentQuestion",
            "AssessmentRun",
            "AssessmentUserResponse",
            "Organization"
        };

        foreach (var entityType in testEntityTypes)
        {
            // Pattern 1: Complete line with DbSet<EntityType>
            content = Regex.Replace(
                content,
                $@"\s*public\s+DbSet<{Regex.Escape(entityType)}>\s+\w+\s*=>\s*Set<{Regex.Escape(entityType)}>\(\);\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            // Pattern 2: Incomplete line (DbSet type was removed but line remains)
            content = Regex.Replace(
                content,
                $@"\s*public\s+\w+\s*=>\s*Set<{Regex.Escape(entityType)}>\(\);\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            // Pattern 3: Any line containing => Set<EntityType>()
            content = Regex.Replace(
                content,
                $@"\s*public\s+[^\r\n]*=>\s*Set<{Regex.Escape(entityType)}>\(\);\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            // Pattern 4: Any remaining DbSet<EntityType> references
            content = Regex.Replace(
                content,
                $@"\s*public\s+DbSet<{Regex.Escape(entityType)}>[^\r\n]*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }

        return content;
    }

    private static string RemoveTestInterfaceReferences(string content)
    {
        // List of Test-related interfaces to remove
        var testInterfaces = new[]
        {
            "IUserTestAttemptRepository",
            "ITestRepository",
            "ITestResultRepository",
            "ITestSubmissionRepository",
            "IAssessmentService",
            "IAssessmentQuestionRepository",
            "IQuestionRepository",
            "ITalentRepository",
            "ITalentScoreCalculator",
            "ITalentScoreRepository",
            "IOrganizationAnalysisService",
            "IReportGenerator"
        };

        foreach (var interfaceName in testInterfaces)
        {
            // Remove using statements
            content = Regex.Replace(
                content,
                $@"using\s+[^;]*\b{Regex.Escape(interfaceName)}\b[^;]*;\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );

            // Find and remove field declarations, then remove their assignments
            var fieldMatches = Regex.Matches(
                content,
                $@"private\s+(readonly\s+)?{Regex.Escape(interfaceName)}\s+(_[a-zA-Z0-9_]+)\s*;\s*\r?\n",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );

            foreach (Match fieldMatch in fieldMatches)
            {
                var fieldName = fieldMatch.Groups[2].Value;
                
                // Remove field declaration
                content = content.Replace(fieldMatch.Value, string.Empty);
                
                // Remove field assignment in constructor
                content = Regex.Replace(
                    content,
                    $@"{Regex.Escape(fieldName)}\s*=\s*[a-zA-Z0-9_]+\s*;\s*\r?\n",
                    string.Empty,
                    RegexOptions.IgnoreCase | RegexOptions.Multiline
                );
            }

            // Remove constructor parameters (handle multi-line and single-line)
            // Pattern: , IInterfaceName parameterName
            content = Regex.Replace(
                content,
                $@"\s*,\s*{Regex.Escape(interfaceName)}\s+[a-zA-Z0-9_]+\s*",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            // Pattern: (IInterfaceName parameterName)
            content = Regex.Replace(
                content,
                $@"\({Regex.Escape(interfaceName)}\s+[a-zA-Z0-9_]+\s*\)",
                "()",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            // Pattern: (IInterfaceName parameterName,
            content = Regex.Replace(
                content,
                $@"\({Regex.Escape(interfaceName)}\s+[a-zA-Z0-9_]+\s*,\s*",
                "(",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }

        // Remove Test-related code blocks (like test attempt handling in GetUserInvoiceDetailsQuery)
        // ONLY apply to files that contain GetUserInvoiceDetailsQuery or invoice.ToDetailDto
        if (content.Contains("GetUserInvoiceDetailsQuery", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("invoice.ToDetailDto()", StringComparison.OrdinalIgnoreCase))
        {
            // Remove the entire block from "var testItem" to the end of the if block
            // This pattern matches: var testItem = ...; if (testItem is not null) { ... }
            content = Regex.Replace(
                content,
                @"var\s+testItem\s*=\s*detail\.Items\.FirstOrDefault\([^)]+InvoiceItemType\.Test[^)]*\);\s*\r?\n\s*if\s*\(testItem\s+is\s+not\s+null\)\s*\{[^}]*var\s+attempt\s*=[^}]*\}\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );
            
            // Also handle the case where the block might be formatted differently
            content = Regex.Replace(
                content,
                @"var\s+testItem\s*=\s*[^;]*InvoiceItemType\.Test[^;]*;\s*\r?\n\s*if\s*\(testItem\s+is\s+not\s+null\)\s*\{[^}]*\}\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );

            // Remove TestAttemptId and TestAttemptStatus from record with expressions
            // Pattern: detail = detail with { ..., TestAttemptId = ..., TestAttemptStatus = ... }
            content = Regex.Replace(
                content,
                @"detail\s*=\s*detail\s+with\s*\{[^}]*TestAttemptId\s*=[^}]*TestAttemptStatus\s*=[^}]*\}\s*;",
                "// Test attempt handling removed",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );
            content = Regex.Replace(
                content,
                @"detail\s*=\s*detail\s+with\s*\{[^}]*TestAttemptStatus\s*=[^}]*TestAttemptId\s*=[^}]*\}\s*;",
                "// Test attempt handling removed",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );
            // If only one property remains, remove the whole with expression
            content = Regex.Replace(
                content,
                @"detail\s*=\s*detail\s+with\s*\{\s*TestAttemptId\s*=[^}]+\}\s*;",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );
            content = Regex.Replace(
                content,
                @"detail\s*=\s*detail\s+with\s*\{\s*TestAttemptStatus\s*=[^}]+\}\s*;",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );
        }

        // Remove TestAttemptId and TestAttemptStatus from individual property assignments
        content = Regex.Replace(
            content,
            @",\s*TestAttemptId\s*=\s*[^,}]+\s*",
            string.Empty,
            RegexOptions.IgnoreCase
        );
        content = Regex.Replace(
            content,
            @",\s*TestAttemptStatus\s*=\s*[^,}]+\s*",
            string.Empty,
            RegexOptions.IgnoreCase
        );

        // Remove any remaining references to attempt variable
        content = Regex.Replace(
            content,
            @"var\s+attempt\s*=\s*await\s+_[a-zA-Z0-9_]+\.GetByInvoiceIdAsync\([^)]+\);\s*\r?\n\s*if\s*\(attempt\s+is\s+not\s+null\)\s*\{[^}]*\}\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );

        // Remove TestAttemptId and TestAttemptStatus from DTO record definitions
        // Pattern: Guid? TestAttemptId = null, TestAttemptStatus? TestAttemptStatus = null
        content = Regex.Replace(
            content,
            @",\s*Guid\?\s+TestAttemptId\s*=\s*null\s*",
            string.Empty,
            RegexOptions.IgnoreCase
        );
        content = Regex.Replace(
            content,
            @",\s*TestAttemptStatus\?\s+TestAttemptStatus\s*=\s*null\s*",
            string.Empty,
            RegexOptions.IgnoreCase
        );
        // Handle if they're at the end of the record
        content = Regex.Replace(
            content,
            @"Guid\?\s+TestAttemptId\s*=\s*null\s*,\s*TestAttemptStatus\?\s+TestAttemptStatus\s*=\s*null\s*\)",
            ")",
            RegexOptions.IgnoreCase
        );
        content = Regex.Replace(
            content,
            @"TestAttemptStatus\?\s+TestAttemptStatus\s*=\s*null\s*\)",
            ")",
            RegexOptions.IgnoreCase
        );
        content = Regex.Replace(
            content,
            @"Guid\?\s+TestAttemptId\s*=\s*null\s*\)",
            ")",
            RegexOptions.IgnoreCase
        );

        // Remove using statements for TestAttemptStatus enum
        content = Regex.Replace(
            content,
            @"using\s+[^;]*\.Enums[^;]*;\s*\r?\n",
            match =>
            {
                // Only remove if it's likely related to Test enums
                // We'll keep Domain.Enums for other enums like InvoiceStatus
                var usingLine = match.Value;
                if (usingLine.Contains("TestAttemptStatus", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }
                return usingLine;
            },
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        // Remove using statements for Test/Assessment related namespaces
        var testNamespaces = new[]
        {
            @"using\s+[^;]*\.Entities\.Assessments[^;]*;\s*\r?\n",
            @"using\s+[^;]*\.Entities\.Tests[^;]*;\s*\r?\n",
            @"using\s+[^;]*\.Application\.Assessments[^;]*;\s*\r?\n",
            @"using\s+[^;]*\.Services\.Assessments[^;]*;\s*\r?\n"
        };

        foreach (var pattern in testNamespaces)
        {
            content = Regex.Replace(
                content,
                pattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }

        // Remove DbSet declarations for Test/Assessment entities (complete lines)
        var testEntityDbSets = new[]
        {
            @"public\s+DbSet<Talent>\s+Talents\s*=>\s*Set<Talent>\(\);\s*\r?\n",
            @"public\s+DbSet<Question>\s+Questions\s*=>\s*Set<Question>\(\);\s*\r?\n",
            @"public\s+DbSet<UserResponse>\s+UserResponses\s*=>\s*Set<UserResponse>\(\);\s*\r?\n",
            @"public\s+DbSet<TalentScore>\s+TalentScores\s*=>\s*Set<TalentScore>\(\);\s*\r?\n",
            @"public\s+DbSet<Test>\s+Tests\s*=>\s*Set<Test>\(\);\s*\r?\n",
            @"public\s+DbSet<TestQuestion>\s+TestQuestions\s*=>\s*Set<TestQuestion>\(\);\s*\r?\n",
            @"public\s+DbSet<TestQuestionOption>\s+TestQuestionOptions\s*=>\s*Set<TestQuestionOption>\(\);\s*\r?\n",
            @"public\s+DbSet<UserTestAttempt>\s+UserTestAttempts\s*=>\s*Set<UserTestAttempt>\(\);\s*\r?\n",
            @"public\s+DbSet<UserTestAnswer>\s+UserTestAnswers\s*=>\s*Set<UserTestAnswer>\(\);\s*\r?\n",
            @"public\s+DbSet<TestResult>\s+TestResults\s*=>\s*Set<TestResult>\(\);\s*\r?\n",
            @"public\s+DbSet<AssessmentQuestion>\s+AssessmentQuestions\s*=>\s*Set<AssessmentQuestion>\(\);\s*\r?\n",
            @"public\s+DbSet<AssessmentRun>\s+AssessmentRuns\s*=>\s*Set<AssessmentRun>\(\);\s*\r?\n",
            @"public\s+DbSet<AssessmentUserResponse>\s+AssessmentResponses\s*=>\s*Set<AssessmentUserResponse>\(\);\s*\r?\n",
            @"public\s+DbSet<Organization>\s+Organizations\s*=>\s*Set<Organization>\(\);\s*\r?\n"
        };

        // Remove references to OrganizationStatus enum and Organization entity
        // Remove OrganizationStatus enum values
        content = Regex.Replace(
            content,
            @"OrganizationStatus\.[a-zA-Z]+\s*",
            "1", // Replace with default value
            RegexOptions.IgnoreCase
        );
        // Remove OrganizationStatus type references
        content = Regex.Replace(
            content,
            @"OrganizationStatus\?\s*",
            "int?",
            RegexOptions.IgnoreCase
        );
        content = Regex.Replace(
            content,
            @":\s*OrganizationStatus\s*",
            ": int",
            RegexOptions.IgnoreCase
        );
        // Remove enum definition for OrganizationStatus (at end of file)
        content = Regex.Replace(
            content,
            @"\r?\n\s*public\s+enum\s+OrganizationStatus\s*\{[^}]+\}\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        // Remove Organization entity type references (only specific patterns)
        // Remove DbSet<Organization>
        content = Regex.Replace(
            content,
            @"DbSet<Organization>",
            string.Empty,
            RegexOptions.IgnoreCase
        );
        // Remove IEntityTypeConfiguration<Organization>
        content = Regex.Replace(
            content,
            @"IEntityTypeConfiguration<Organization>",
            string.Empty,
            RegexOptions.IgnoreCase
        );
        // Remove EntityTypeBuilder<Organization>
        content = Regex.Replace(
            content,
            @"EntityTypeBuilder<Organization>",
            string.Empty,
            RegexOptions.IgnoreCase
        );
        // Remove Organization parameter types
        content = Regex.Replace(
            content,
            @"\bOrganization\s+[a-zA-Z0-9_]+\s*[=,)]",
            match => match.Value.Replace("Organization ", string.Empty),
            RegexOptions.IgnoreCase
        );

        foreach (var pattern in testEntityDbSets)
        {
            content = Regex.Replace(
                content,
                pattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }

        // Additional cleanup for Organization - handle any remaining references
        // Remove any remaining DbSet<Organization> lines (in case regex didn't match)
        content = Regex.Replace(
            content,
            @"\s*public\s+DbSet<Organization>[^\r\n]*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        // Remove references to Assessment/Test related classes
        var testClasses = new[]
        {
            "MatrixLoader",
            "CorrelationMatrixProvider",
            "IScoringStrategy",
            "MeanOverMaxStrategy",
            "WeightedRatioStrategy",
            "IScoringStrategyResolver",
            "ScoringStrategyResolver"
        };

        foreach (var className in testClasses)
        {
            // Remove using statements
            content = Regex.Replace(
                content,
                $@"using\s+[^;]*\b{Regex.Escape(className)}\b[^;]*;\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }

        // Remove OnModelCreating code related to Test/Assessment entities
        // Remove modelBuilder.Entity<AssessmentQuestion> configurations
        content = Regex.Replace(
            content,
            @"modelBuilder\.Entity<AssessmentQuestion>\(\)[^;]*;\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        // Remove modelBuilder.Entity<AssessmentUserResponse> configurations
        content = Regex.Replace(
            content,
            @"modelBuilder\.Entity<AssessmentUserResponse>\(\)[^;]*;\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        // Remove modelBuilder.Entity<Talent> seed data
        content = Regex.Replace(
            content,
            @"modelBuilder\.Entity<Talent>\(\)\.HasData\([^)]+\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        // Remove modelBuilder.Entity<Question> seed data
        content = Regex.Replace(
            content,
            @"modelBuilder\.Entity<Question>\(\)\.HasData\([^)]+\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        // Remove SeedData references to Test entities
        content = Regex.Replace(
            content,
            @"SeedData\.Talents\s*",
            "Array.Empty<object>()",
            RegexOptions.IgnoreCase
        );
        content = Regex.Replace(
            content,
            @"SeedData\.Questions\s*",
            "Array.Empty<object>()",
            RegexOptions.IgnoreCase
        );

        // Fix double closing braces issue (remove extra }}) - ONLY for GetUserInvoiceDetailsQuery
        // This should only be applied to files that have the specific pattern
        if (content.Contains("GetUserInvoiceDetailsQuery", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("invoice.ToDetailDto()", StringComparison.OrdinalIgnoreCase))
        {
            // Pattern: } followed by } followed by return statement
            // This happens when Test block is removed but leaves extra closing braces
            content = Regex.Replace(
                content,
                @"(\s+)\}\s*\r?\n\s*\}\s*\r?\n\s*return\s+Result",
                "$1return Result",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            // Handle case with more whitespace or multiple closing braces
            content = Regex.Replace(
                content,
                @"(\s+)\}\s*\r?\n\s*\}\s*\r?\n\s*\}\s*\r?\n\s*return",
                "$1return",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            // Also handle case where detail = detail with { ... } was removed, leaving just }}
            content = Regex.Replace(
                content,
                @"var\s+detail\s*=\s*invoice\.ToDetailDto\(\);\s*\r?\n\s*\r?\n\s*\}\s*\r?\n\s*\}\s*\r?\n\s*return",
                "var detail = invoice.ToDetailDto();\r\n\r\n        return",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }

        return content;
    }

    private static string RemoveTestServiceRegistrations(string content)
    {
        // Remove service registrations for Test/Assessment/Report related services
        var servicesToRemove = new[]
        {
            @"services\.AddScoped<IQuestionRepository[^;]+;\s*\r?\n",
            @"services\.AddScoped<ITalentRepository[^;]+;\s*\r?\n",
            @"services\.AddScoped<ITestSubmissionRepository[^;]+;\s*\r?\n",
            @"services\.AddScoped<ITalentScoreRepository[^;]+;\s*\r?\n",
            @"services\.AddScoped<ITalentScoreCalculator[^;]+;\s*\r?\n",
            @"services\.AddScoped<IReportGenerator[^;]+;\s*\r?\n",
            @"services\.AddScoped<IOrganizationAnalysisService[^;]+;\s*\r?\n",
            @"services\.AddScoped<IAssessmentService[^;]+;\s*\r?\n",
            @"services\.AddScoped<IAssessmentQuestionRepository[^;]+;\s*\r?\n",
            @"services\.AddScoped<ITestRepository[^;]+;\s*\r?\n",
            @"services\.AddScoped<ITestResultRepository[^;]+;\s*\r?\n",
            @"services\.AddScoped<IUserTestAttemptRepository[^;]+;\s*\r?\n",
            // Remove MatrixLoader and related services
            @"services\.AddSingleton<MatrixLoader>\(\);\s*\r?\n",
            @"services\.AddSingleton<CorrelationMatrixProvider>\(\);\s*\r?\n",
            @"services\.AddSingleton<IScoringStrategy,\s*MeanOverMaxStrategy>\(\);\s*\r?\n",
            @"services\.AddSingleton<IScoringStrategy,\s*WeightedRatioStrategy>\(\);\s*\r?\n",
            @"services\.AddSingleton<IScoringStrategyResolver,\s*ScoringStrategyResolver>\(\);\s*\r?\n"
        };

        foreach (var pattern in servicesToRemove)
        {
            content = Regex.Replace(
                content,
                pattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }

        return content;
    }

    private static string RemoveBuildLearningMetricsAsync(string content)
    {
        // Remove BuildLearningMetricsAsync method from AdminDashboardMetricsService
        // Match from "private async Task<LearningMetricsDto> BuildLearningMetricsAsync" to the closing brace
        var methodPattern = @"private\s+async\s+Task<LearningMetricsDto>\s+BuildLearningMetricsAsync\s*\([^)]+\)\s*\{";
        var methodMatch = Regex.Match(content, methodPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        if (methodMatch.Success)
        {
            var startIndex = methodMatch.Index;
            // Find the start of the line
            var lineStart = startIndex;
            while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                lineStart--;
            
            var braceStart = content.IndexOf('{', startIndex);
            if (braceStart >= 0)
            {
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                
                if (braceCount == 0)
                {
                    // Include newline after closing brace
                    while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                        i++;
                    
                    // Remove from line start to end of method
                    content = content.Remove(lineStart, i - lineStart);
                }
            }
        }
        
        // Remove the variable declaration: var learning = await BuildLearningMetricsAsync(...);
        content = Regex.Replace(
            content,
            @"\s*var\s+learning\s*=\s*await\s+BuildLearningMetricsAsync\([^)]+\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        // Remove learning from SystemPerformanceSummaryDto constructor call
        // The constructor call is: new SystemPerformanceSummaryDto(period, people, commerce, learning, content, now)
        // After removing learning, it should be: new SystemPerformanceSummaryDto(period, people, commerce, content, now)
        // Match the pattern: commerce, followed by newline/whitespace, learning, followed by comma and newline/whitespace, content
        content = Regex.Replace(
            content,
            @"commerce\s*,\s*\r?\n\s*learning\s*,\s*\r?\n\s*content",
            "commerce,\r\n            content",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Also handle single-line format
        content = Regex.Replace(
            content,
            @"commerce\s*,\s*learning\s*,\s*content",
            "commerce, content",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        return content;
    }

    private static string RemoveLearningMetricsFromSystemPerformanceSummaryDto(string content)
    {
        // Remove LearningMetricsDto parameter from SystemPerformanceSummaryDto constructor
        // The constructor is: SystemPerformanceSummaryDto(PeriodWindowDto, PeopleMetricsDto, CommerceMetricsDto, LearningMetricsDto, ContentMetricsDto, DateTimeOffset)
        // After removal: SystemPerformanceSummaryDto(PeriodWindowDto, PeopleMetricsDto, CommerceMetricsDto, ContentMetricsDto, DateTimeOffset)
        
        // Pattern 1: Remove LearningMetricsDto Learning, line (with comma and newline)
        content = Regex.Replace(
            content,
            @"\s*LearningMetricsDto\s+Learning\s*,?\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Pattern 2: Remove LearningMetricsDto Learning, if it's between CommerceMetricsDto and ContentMetricsDto
        content = Regex.Replace(
            content,
            @"CommerceMetricsDto\s+Commerce\s*,?\s*\r?\n\s*LearningMetricsDto\s+Learning\s*,?\s*\r?\n\s*ContentMetricsDto",
            "CommerceMetricsDto Commerce,\r\n    ContentMetricsDto",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Also remove LearningMetricsDto record definition if it exists in the same file
        // Match from "public sealed record LearningMetricsDto(" to the closing ");"
        var learningMetricsRecordPattern = @"public\s+sealed\s+record\s+LearningMetricsDto\s*\(";
        var learningMetricsMatch = Regex.Match(content, learningMetricsRecordPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        if (learningMetricsMatch.Success)
        {
            var startIndex = learningMetricsMatch.Index;
            var lineStart = startIndex;
            while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                lineStart--;
            
            var parenStart = content.IndexOf('(', startIndex);
            if (parenStart >= 0)
            {
                int parenCount = 1;
                int i = parenStart + 1;
                while (i < content.Length && parenCount > 0)
                {
                    if (content[i] == '(') parenCount++;
                    else if (content[i] == ')') parenCount--;
                    i++;
                }
                
                if (parenCount == 0)
                {
                    // Find semicolon after closing parenthesis
                    while (i < content.Length && char.IsWhiteSpace(content[i]))
                        i++;
                    if (i < content.Length && content[i] == ';')
                        i++;
                    // Include newline
                    while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                        i++;
                    
                    // Remove from line start to end of record
                    content = content.Remove(lineStart, i - lineStart);
                }
            }
        }

        return content;
    }

    private static string RemoveAssessmentCode(string content)
    {
        // Remove Assessment-related using statements
        content = Regex.Replace(
            content,
            @"using\s+[^;]*\.Assessments[^;]*;\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        content = Regex.Replace(
            content,
            @"using\s+[^;]*\.Entities\.Assessments[^;]*;\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Remove Growth namespace using statement
        content = Regex.Replace(
            content,
            @"using\s+[^;]*\.Growth[^;]*;\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        // Remove Assessment service registrations using parenthesis matching for accuracy
        // Remove MatricesOptions configuration
        var matricesPattern = @"builder\.Services\.Configure<MatricesOptions>\s*\(";
        var matricesMatch = Regex.Match(content, matricesPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        while (matricesMatch.Success)
        {
            var startIndex = matricesMatch.Index;
            var lineStart = startIndex;
            while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                lineStart--;
            
            var parenStart = content.IndexOf('(', startIndex);
            if (parenStart >= 0)
            {
                int parenCount = 1;
                int i = parenStart + 1;
                bool inString = false;
                char stringChar = '\0';
                
                while (i < content.Length && parenCount > 0)
                {
                    if (!inString)
                    {
                        if (content[i] == '"' || content[i] == '\'')
                        {
                            inString = true;
                            stringChar = content[i];
                        }
                        else if (content[i] == '(') parenCount++;
                        else if (content[i] == ')') parenCount--;
                    }
                    else
                    {
                        if (content[i] == stringChar && (i == 0 || content[i - 1] != '\\'))
                            inString = false;
                    }
                    i++;
                }
                
                if (parenCount == 0)
                {
                    // Find semicolon
                    while (i < content.Length && char.IsWhiteSpace(content[i]))
                        i++;
                    if (i < content.Length && content[i] == ';')
                        i++;
                    // Include newline
                    while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                        i++;
                    
                    // Remove the entire line
                    content = content.Remove(lineStart, i - lineStart);
                    matricesMatch = Regex.Match(content, matricesPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
        
        // Remove ScoringOptions configuration
        var scoringPattern = @"builder\.Services\.Configure<ScoringOptions>\s*\(";
        var scoringMatch = Regex.Match(content, scoringPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        while (scoringMatch.Success)
        {
            var startIndex = scoringMatch.Index;
            var lineStart = startIndex;
            while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                lineStart--;
            
            var parenStart = content.IndexOf('(', startIndex);
            if (parenStart >= 0)
            {
                int parenCount = 1;
                int i = parenStart + 1;
                bool inString = false;
                char stringChar = '\0';
                
                while (i < content.Length && parenCount > 0)
                {
                    if (!inString)
                    {
                        if (content[i] == '"' || content[i] == '\'')
                        {
                            inString = true;
                            stringChar = content[i];
                        }
                        else if (content[i] == '(') parenCount++;
                        else if (content[i] == ')') parenCount--;
                    }
                    else
                    {
                        if (content[i] == stringChar && (i == 0 || content[i - 1] != '\\'))
                            inString = false;
                    }
                    i++;
                }
                
                if (parenCount == 0)
                {
                    // Find semicolon
                    while (i < content.Length && char.IsWhiteSpace(content[i]))
                        i++;
                    if (i < content.Length && content[i] == ';')
                        i++;
                    // Include newline
                    while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                        i++;
                    
                    // Remove the entire line
                    content = content.Remove(lineStart, i - lineStart);
                    scoringMatch = Regex.Match(content, scoringPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
        
        // Remove orphaned closing parentheses and semicolons (leftover from removed lines)
        // Match standalone ); on a line (with optional whitespace)
        content = Regex.Replace(
            content,
            @"^\s*\);\s*\r?\n",
            string.Empty,
            RegexOptions.Multiline
        );
        
        // Also remove ); that might be on the same line as other content but orphaned
        // Match ); followed by newline or end of line, but not part of a valid statement
        // This handles cases where ); is left alone on a line
        content = Regex.Replace(
            content,
            @"\r?\n\s*\);\s*\r?\n",
            "\r\n",
            RegexOptions.Multiline
        );
        
        // Remove ); at the start of a line (after previous removals)
        content = Regex.Replace(
            content,
            @"^\s*\);\s*$",
            string.Empty,
            RegexOptions.Multiline
        );
        content = Regex.Replace(
            content,
            @"builder\.Services\.AddSingleton<[^>]*MatrixLoader[^>]*>\([^}]+}\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        // Remove IQuestionImporter service registration
        // Pattern: builder.Services.AddScoped<IQuestionImporter, QuestionImporter>();
        content = Regex.Replace(
            content,
            @"builder\.Services\.AddScoped<IQuestionImporter\s*,\s*QuestionImporter\s*>\s*\([^)]*\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        // Also match without generic type parameters (fallback)
        content = Regex.Replace(
            content,
            @"builder\.Services\.AddScoped<IQuestionImporter[^>]*>\s*\([^)]+\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        // Remove Growth.AssessmentService service registration (matches both aa.WebSite.Growth.AssessmentService and Growth.AssessmentService)
        content = Regex.Replace(
            content,
            @"builder\.Services\.AddScoped<[^>]*Growth\.AssessmentService[^>]*>\([^)]*\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Remove MatrixLoader service registration
        // Match: builder.Services.AddSingleton(sp => { ... return MatrixLoader.Load(...); });
        var matrixLoaderPattern = @"builder\.Services\.AddSingleton\s*\(\s*sp\s*=>\s*\{";
        var matrixLoaderMatch = Regex.Match(content, matrixLoaderPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        while (matrixLoaderMatch.Success)
        {
            var startIndex = matrixLoaderMatch.Index;
            var lineStart = startIndex;
            while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                lineStart--;
            
            var braceStart = content.IndexOf('{', startIndex);
            if (braceStart >= 0)
            {
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                
                if (braceCount == 0)
                {
                    // Check if this block contains MatrixLoader.Load
                    var blockContent = content.Substring(braceStart + 1, i - 1 - braceStart - 1);
                    if (blockContent.Contains("MatrixLoader.Load", StringComparison.OrdinalIgnoreCase))
                    {
                        // Find semicolon and closing parenthesis after closing brace
                        while (i < content.Length && char.IsWhiteSpace(content[i]))
                            i++;
                        if (i < content.Length && content[i] == ')')
                            i++;
                        while (i < content.Length && char.IsWhiteSpace(content[i]))
                            i++;
                        if (i < content.Length && content[i] == ';')
                            i++;
                        // Include newline
                        while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                            i++;
                        
                        // Remove from line start to end of AddSingleton call
                        content = content.Remove(lineStart, i - lineStart);
                        matrixLoaderMatch = Regex.Match(content, matrixLoaderPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
        
        // Remove IQuestionImporter service registration
        // Pattern: builder.Services.AddScoped<IQuestionImporter, QuestionImporter>();
        content = Regex.Replace(
            content,
            @"builder\.Services\.AddScoped<IQuestionImporter\s*,\s*QuestionImporter\s*>\s*\([^)]*\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        // Also match without generic type parameters
        content = Regex.Replace(
            content,
            @"builder\.Services\.AddScoped<IQuestionImporter[^>]*>\s*\([^)]+\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Remove AssessmentService from Growth namespace
        content = Regex.Replace(
            content,
            @"builder\.Services\.AddScoped<[^>]*\.Growth\.AssessmentService[^>]*>\([^)]+\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        // Remove Assessment initialization code (the using block with IQuestionImporter)
        // Match: using (var scope = app.Services.CreateScope()) { ... IQuestionImporter ... AssessmentQuestions ... }
        var usingBlockPattern = @"using\s*\(\s*var\s+scope\s*=\s*app\.Services\.CreateScope\(\)\s*\)\s*\{";
        var usingBlockMatch = Regex.Match(content, usingBlockPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        if (usingBlockMatch.Success)
        {
            var startIndex = usingBlockMatch.Index;
            var lineStart = startIndex;
            while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                lineStart--;
            
            var braceStart = content.IndexOf('{', startIndex);
            if (braceStart >= 0)
            {
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                
                if (braceCount == 0)
                {
                    // Check if this block contains IQuestionImporter or AssessmentQuestions
                    var blockContent = content.Substring(braceStart + 1, i - 1 - braceStart - 1);
                    if (blockContent.Contains("IQuestionImporter", StringComparison.OrdinalIgnoreCase) ||
                        blockContent.Contains("AssessmentQuestions", StringComparison.OrdinalIgnoreCase) ||
                        blockContent.Contains("ImportCliftonAsync", StringComparison.OrdinalIgnoreCase) ||
                        blockContent.Contains("ImportPvqAsync", StringComparison.OrdinalIgnoreCase))
                    {
                        // Include newline after closing brace
                        while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                            i++;
                        
                        // Remove from line start to end of using block
                        content = content.Remove(lineStart, i - lineStart);
                    }
                }
            }
        }
        
        // Also remove the old pattern if it exists
        content = Regex.Replace(
            content,
            @"var\s+hasClifton\s*=[^;]+;\s*\r?\n\s*var\s+hasPvq\s*=[^;]+;\s*\r?\n\s*if\s*\([^}]+}\)\s*\{[^}]+\}\s*\r?\n\s*if\s*\([^}]+}\)\s*\{[^}]+\}\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );

        // Remove Assessment API from Swagger
        content = Regex.Replace(
            content,
            @"Title\s*=\s*""Assessment API"",\s*\r?\n\s*Version\s*=\s*""v1"",\s*\r?\n\s*Description\s*=\s*""Clifton\s*\+\s*Schwartz[^""]+""",
            @"Title = ""API"",\r\n        Version = ""v1"",\r\n        Description = ""API Documentation""",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        content = Regex.Replace(
            content,
            @"options\.SwaggerEndpoint\([^)]+""Assessment API[^)]+\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Remove the specific Swagger configuration block
        // Match: builder.Services.AddSwaggerGen(options => { options.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1", Description = "API Documentation" }); });
        var swaggerBlockPattern = @"builder\.Services\.AddSwaggerGen\s*\(\s*options\s*=>\s*\{";
        var swaggerMatch = Regex.Match(content, swaggerBlockPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        if (swaggerMatch.Success)
        {
            var startIndex = swaggerMatch.Index;
            var lineStart = startIndex;
            while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                lineStart--;
            
            var braceStart = content.IndexOf('{', startIndex);
            if (braceStart >= 0)
            {
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                
                if (braceCount == 0)
                {
                    // Check if this block contains the specific SwaggerDoc configuration
                    var blockContent = content.Substring(braceStart + 1, i - 1 - braceStart - 1);
                    if (blockContent.Contains("Title = \"API\"", StringComparison.OrdinalIgnoreCase) &&
                        blockContent.Contains("Version = \"v1\"", StringComparison.OrdinalIgnoreCase) &&
                        blockContent.Contains("Description = \"API Documentation\"", StringComparison.OrdinalIgnoreCase))
                    {
                        // Find semicolon after closing brace
                        while (i < content.Length && char.IsWhiteSpace(content[i]))
                            i++;
                        if (i < content.Length && content[i] == ';')
                            i++;
                        // Include newline
                        while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                            i++;
                        
                        // Remove from line start to end of AddSwaggerGen call
                        content = content.Remove(lineStart, i - lineStart);
                    }
                }
            }
        }

        // Remove Matrices and Scoring configuration from appsettings (if present in Program.cs comments)
        content = Regex.Replace(
            content,
            @"""Matrices"":[^}]+},\s*",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        content = Regex.Replace(
            content,
            @"""Scoring"":[^}]+},\s*",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );

        // Final cleanup: Remove any remaining orphaned ); patterns
        // This is a catch-all to ensure no orphaned ); remains after all removals
        
        // Remove standalone ); lines (with optional whitespace before and after)
        // This handles lines that contain only ); with optional whitespace
        content = Regex.Replace(
            content,
            @"^\s*\);\s*$",
            string.Empty,
            RegexOptions.Multiline
        );
        
        // Remove lines that contain only ); with optional whitespace (including newlines)
        // Pattern: newline, optional whitespace, );, optional whitespace, newline
        content = Regex.Replace(
            content,
            @"\r?\n\s*\);\s*\r?\n",
            "\r\n",
            RegexOptions.Multiline
        );
        
        // Remove orphaned ); that might be on a line by itself after other code
        // Pattern: newline, optional whitespace, );, optional whitespace, followed by newline or end of string
        content = Regex.Replace(
            content,
            @"\r?\n\s*\);\s*(?=\r?\n|$)",
            string.Empty,
            RegexOptions.Multiline
        );
        
        // Additional cleanup: Remove ); that appears alone on a line (more aggressive)
        // This handles cases where ); is on its own line but might have different whitespace patterns
        content = Regex.Replace(
            content,
            @"(?<=\r?\n)\s*\);\s*(?=\r?\n)",
            string.Empty,
            RegexOptions.Multiline
        );
        
        // Remove ); at the start of a line (after previous removals)
        content = Regex.Replace(
            content,
            @"^\s*\);\s*$",
            string.Empty,
            RegexOptions.Multiline
        );

        // Additional pass: Remove any remaining orphaned ); that might have been missed
        // This is a more aggressive cleanup to catch any remaining cases
        var orphanedPattern = @"\r?\n\s*\);\s*(?=\r?\n|$)";
        var orphanedMatch = Regex.Match(content, orphanedPattern, RegexOptions.Multiline);
        while (orphanedMatch.Success)
        {
            content = Regex.Replace(
                content,
                orphanedPattern,
                string.Empty,
                RegexOptions.Multiline
            );
            orphanedMatch = Regex.Match(content, orphanedPattern, RegexOptions.Multiline);
        }
        
        // Remove AddEndpointsApiExplorer
        content = Regex.Replace(
            content,
            @"builder\.Services\.AddEndpointsApiExplorer\s*\([^)]*\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        // Remove app.UseSwagger()
        content = Regex.Replace(
            content,
            @"app\.UseSwagger\s*\([^)]*\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        // Final aggressive cleanup: Remove any remaining orphaned ); 
        // This must be done after all other removals to catch any remaining cases
        // Try multiple patterns to ensure we catch all cases
        var finalCleanupPatterns = new[]
        {
            @"\r?\n\s*\);\s*\r?\n",           // ); on its own line with newlines
            @"\r?\n\s*\);\s*$",                 // ); at end of file
            @"^\s*\);\s*\r?\n",                 // ); at start of line
            @"\r?\n\s*\);\s*(?=\r?\n|$)",       // ); followed by newline or end
            @"(?<=\r?\n)\s*\);\s*(?=\r?\n)",    // ); between two newlines
        };
        
        foreach (var pattern in finalCleanupPatterns)
        {
            var match = Regex.Match(content, pattern, RegexOptions.Multiline);
            while (match.Success)
            {
                content = Regex.Replace(content, pattern, string.Empty, RegexOptions.Multiline);
                match = Regex.Match(content, pattern, RegexOptions.Multiline);
            }
        }
        
        // Remove app.UseSwaggerUI block
        var swaggerUIPattern = @"app\.UseSwaggerUI\s*\(\s*options\s*=>\s*\{";
        var swaggerUIMatch = Regex.Match(content, swaggerUIPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        while (swaggerUIMatch.Success)
        {
            var startIndex = swaggerUIMatch.Index;
            var lineStart = startIndex;
            while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                lineStart--;
            
            var braceStart = content.IndexOf('{', startIndex);
            if (braceStart >= 0)
            {
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                
                if (braceCount == 0)
                {
                    // Find semicolon after closing brace
                    while (i < content.Length && char.IsWhiteSpace(content[i]))
                        i++;
                    if (i < content.Length && content[i] == ';')
                        i++;
                    // Include newline
                    while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                        i++;
                    
                    // Remove from line start to end of UseSwaggerUI call
                    content = content.Remove(lineStart, i - lineStart);
                    swaggerUIMatch = Regex.Match(content, swaggerUIPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        // Final pass: One more aggressive cleanup for orphaned ); at the very end
        // This ensures we catch any ); that might have been left after all other processing
        content = Regex.Replace(
            content,
            @"\r?\n\s*\);\s*(?=\r?\n|$)",
            string.Empty,
            RegexOptions.Multiline
        );
        
        // Remove any standalone ); lines one more time
        content = Regex.Replace(
            content,
            @"^\s*\);\s*$",
            string.Empty,
            RegexOptions.Multiline
        );

        return content;
    }
    
    private static string RemoveMetricsRoute(string content)
    {
        // Remove /metrics route mapping
        // Pattern: app.MapGet("/metrics", ...) or app.MapControllerRoute with metrics
        content = Regex.Replace(
            content,
            @"app\.MapGet\s*\(\s*""/metrics""[^)]+\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        // Remove MapControllerRoute for metrics
        content = Regex.Replace(
            content,
            @"app\.MapControllerRoute\s*\([^)]*""/metrics""[^)]+\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        // Remove route pattern containing /metrics
        content = Regex.Replace(
            content,
            @"app\.Map\w+\s*\([^)]*""/metrics""[^)]+\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );

        return content;
    }
    
    private static string IgnoreNonExistentPropertiesInConfiguration(string content)
    {
        // Properties that don't exist in database but might be in entity classes
        // Note: UpdaterId, CreatorId, IsDeleted, and RemoveDate are kept as they might be needed
        var propertiesToIgnore = new[]
        {
            "Tags"
        };

        // Find the Configure method in the configuration class
        var configureMethodPattern = @"public\s+void\s+Configure\s*\([^)]+\)\s*\{";
        var configureMatch = Regex.Match(content, configureMethodPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        if (!configureMatch.Success)
        {
            return content;
        }

        var configureStart = configureMatch.Index;
        var braceStart = content.IndexOf('{', configureStart);
        if (braceStart < 0)
        {
            return content;
        }

        // Find the end of the Configure method
        int braceCount = 1;
        int i = braceStart + 1;
        while (i < content.Length && braceCount > 0)
        {
            if (content[i] == '{') braceCount++;
            else if (content[i] == '}') braceCount--;
            i++;
        }

        if (braceCount != 0)
        {
            return content;
        }

        var configureMethodContent = content.Substring(braceStart + 1, i - 1 - braceStart - 1);
        var beforeMethod = content.Substring(0, braceStart + 1);
        var afterMethod = content.Substring(i - 1);

        // Remove property configurations for properties that don't exist in database
        foreach (var prop in propertiesToIgnore)
        {
            // Remove builder.Property configurations for this property
            // Pattern: builder.Property(e => e.PropertyName)...;
            var propertyConfigPattern = $@"builder\.Property\s*\([^)]*=>\s*[^)]*\.{prop}[^)]*\)[^;]*;\s*\r?\n";
            configureMethodContent = Regex.Replace(
                configureMethodContent,
                propertyConfigPattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );
            
            // Also remove HasOne/HasMany configurations that reference these properties
            var hasOnePattern = $@"builder\.HasOne\s*\([^)]*=>\s*[^)]*\.{prop}[^)]*\)[^;]*;\s*\r?\n";
            configureMethodContent = Regex.Replace(
                configureMethodContent,
                hasOnePattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );
            
            var hasManyPattern = $@"builder\.HasMany\s*\([^)]*=>\s*[^)]*\.{prop}[^)]*\)[^;]*;\s*\r?\n";
            configureMethodContent = Regex.Replace(
                configureMethodContent,
                hasManyPattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );
        }

        // Reconstruct the content
        return beforeMethod + configureMethodContent + afterMethod;
    }
    
    private static string RemoveForeignKeyAttributesFromBaseEntity(string content)
    {
        // Remove ForeignKey attributes for properties that don't exist
        var propertiesToRemove = new[]
        {
            "UpdaterId",
            "CreatorId"
        };

        foreach (var prop in propertiesToRemove)
        {
            // Remove [ForeignKey(nameof(PropertyName))] attribute
            var foreignKeyPattern = $@"\[\s*ForeignKey\s*\(\s*nameof\s*\(\s*{prop}\s*\)\s*\)\s*\]\s*\r?\n";
            content = Regex.Replace(
                content,
                foreignKeyPattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }

        return content;
    }
    
    private static string RemoveNonExistentPropertiesFromEntity(string content)
    {
        // Properties that don't exist in database (but keep UpdaterId, CreatorId, IsDeleted, and RemoveDate as they might be needed)
        var propertiesToRemove = new[]
        {
            "Tags"
        };

        foreach (var prop in propertiesToRemove)
        {
            // Remove public property declarations
            // Pattern: public [type] PropertyName { get; set; } or { get; private set; } etc.
            var propertyPattern = $@"public\s+[^\s]+\s+{prop}\s*{{\s*get[^}}]*}}\s*\r?\n";
            content = Regex.Replace(
                content,
                propertyPattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            
            // Also remove private fields if they exist
            var fieldPattern = $@"private\s+[^\s]+\s+_{prop}[^;]*;\s*\r?\n";
            content = Regex.Replace(
                content,
                fieldPattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }

        return content;
    }
    
    private static string AddUsingStatementsToDetailsView(string content)
    {
        // Add required using statements if not present
        var hasTextJson = content.Contains("@using System.Text.Json", StringComparison.OrdinalIgnoreCase);
        var hasGlobalization = content.Contains("@using System.Globalization", StringComparison.OrdinalIgnoreCase);
        
        if (!hasTextJson || !hasGlobalization)
        {
            // Try to find the best insertion point: after existing @using statements or before @model
            var insertPosition = 0;
            var usingStatements = new List<string>();
            
            // First, try to find existing @using statements and add after them
            var existingUsingPattern = @"(@using\s+[^\r\n]+[\r\n]+)";
            var existingUsingMatches = Regex.Matches(content, existingUsingPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            
            if (existingUsingMatches.Count > 0)
            {
                // Insert after the last @using statement
                var lastUsing = existingUsingMatches[existingUsingMatches.Count - 1];
                insertPosition = lastUsing.Index + lastUsing.Length;
            }
            else
            {
                // If no @using found, try to find @model and insert before it
                var modelPattern = @"@model\s+";
                var modelMatch = Regex.Match(content, modelPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (modelMatch.Success)
                {
                    insertPosition = modelMatch.Index;
                }
                else
                {
                    // If no @model found, insert at the beginning
                    insertPosition = 0;
                }
            }
            
            if (!hasTextJson)
            {
                usingStatements.Add("@using System.Text.Json");
            }
            
            if (!hasGlobalization)
            {
                usingStatements.Add("@using System.Globalization");
            }
            
            if (usingStatements.Count > 0)
            {
                var usingBlock = string.Join("\r\n", usingStatements) + "\r\n";
                content = content.Insert(insertPosition, usingBlock);
            }
        }
        
        return content;
    }
    
    private static string CleanMigrationFile(string content)
    {
        // Remove CreateTable calls for Test/Assessment/Organization related tables
        var testTableNames = new[]
        {
            "Tests",
            "TestQuestions",
            "TestQuestionOptions",
            "TestResults",
            "TestSubmissions",
            "UserTestAttempts",
            "UserTestAnswers",
            "Assessments",
            "AssessmentQuestions",
            "AssessmentRuns",
            "AssessmentResponses",
            "AssessmentUserResponses",
            "Organizations",
            "Talents",
            "TalentScores",
            "Questions"
        };

        foreach (var tableName in testTableNames)
        {
            // Remove CreateTable for this table (with full block using brace matching)
            var createTablePattern = $@"migrationBuilder\.CreateTable\s*\(\s*name:\s*""{tableName}""";
            var createTableMatch = Regex.Match(content, createTablePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            while (createTableMatch.Success)
            {
                var startIndex = createTableMatch.Index;
                var lineStart = startIndex;
                while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                    lineStart--;
                
                // Find the opening parenthesis
                var parenStart = content.IndexOf('(', startIndex);
                if (parenStart >= 0)
                {
                    int parenCount = 1;
                    int i = parenStart + 1;
                    bool inString = false;
                    char stringChar = '\0';
                    
                    while (i < content.Length && parenCount > 0)
                    {
                        if (!inString)
                        {
                            if (content[i] == '"' || content[i] == '\'')
                            {
                                inString = true;
                                stringChar = content[i];
                            }
                            else if (content[i] == '(') parenCount++;
                            else if (content[i] == ')') parenCount--;
                        }
                        else
                        {
                            if (content[i] == stringChar && (i == 0 || content[i - 1] != '\\'))
                                inString = false;
                        }
                        i++;
                    }
                    
                    if (parenCount == 0)
                    {
                        // Find semicolon
                        while (i < content.Length && char.IsWhiteSpace(content[i]))
                            i++;
                        if (i < content.Length && content[i] == ';')
                            i++;
                        // Include newline
                        while (i < content.Length && (content[i] == '\r' || content[i] == '\n'))
                            i++;
                        
                        // Remove from line start to end of CreateTable call
                        content = content.Remove(lineStart, i - lineStart);
                        createTableMatch = Regex.Match(content, createTablePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            // Remove DropTable for this table
            var dropTablePattern = $@"migrationBuilder\.DropTable\s*\(\s*name:\s*""{tableName}""\s*\);\s*\r?\n";
            content = Regex.Replace(
                content,
                dropTablePattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );

            // Remove AddForeignKey for this table
            var addForeignKeyPattern = $@"migrationBuilder\.AddForeignKey\s*\([^)]*""{tableName}""[^)]+\);\s*\r?\n";
            content = Regex.Replace(
                content,
                addForeignKeyPattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );

            // Remove DropForeignKey for this table
            var dropForeignKeyPattern = $@"migrationBuilder\.DropForeignKey\s*\([^)]*""{tableName}""[^)]+\);\s*\r?\n";
            content = Regex.Replace(
                content,
                dropForeignKeyPattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );

            // Remove CreateIndex for this table
            var createIndexPattern = $@"migrationBuilder\.CreateIndex\s*\([^)]*""{tableName}""[^)]+\);\s*\r?\n";
            content = Regex.Replace(
                content,
                createIndexPattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );

            // Remove DropIndex for this table
            var dropIndexPattern = $@"migrationBuilder\.DropIndex\s*\([^)]*""{tableName}""[^)]+\);\s*\r?\n";
            content = Regex.Replace(
                content,
                dropIndexPattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );
        }

        return content;
    }
    
    private static string RemoveDiscountApplicationResultReferences(string content)
    {
        // Extract namespace from content
        var namespaceMatch = Regex.Match(content, @"namespace\s+([^;{]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var fullNamespace = "YourProject.Domain.Entities";
        if (namespaceMatch.Success && namespaceMatch.Groups.Count > 1)
        {
            fullNamespace = namespaceMatch.Groups[1].Value.Trim();
        }
        
        // Extract root namespace
        var rootNamespace = fullNamespace.Split('.')[0];
        
        // Check if DiscountApplicationResult record already exists in the file
        var hasDiscountApplicationResult = content.Contains("record DiscountApplicationResult", StringComparison.OrdinalIgnoreCase) ||
                                          content.Contains("class DiscountApplicationResult", StringComparison.OrdinalIgnoreCase);
        
        // If not, add the record definition at the end of the namespace (before closing brace)
        if (!hasDiscountApplicationResult)
        {
            // Find the last closing brace of the namespace
            var lastBraceIndex = content.LastIndexOf('}');
            if (lastBraceIndex > 0)
            {
                // Find the line before the last closing brace
                var insertPosition = lastBraceIndex;
                while (insertPosition > 0 && content[insertPosition - 1] != '\n' && content[insertPosition - 1] != '\r')
                    insertPosition--;
                
                // Add the record definition
                var recordDefinition = $@"
    public sealed record DiscountApplicationResult(
        string Code,
        {rootNamespace}.Domain.Enums.DiscountType AppliedDiscountType,
        decimal AppliedDiscountValue,
        decimal OriginalPrice,
        decimal DiscountAmount,
        string? NormalizedAudienceKey,
        bool WasCapped,
        DateTimeOffset EvaluatedAt,
        decimal? EffectiveMaxDiscount)
    {{
        public bool Success => !string.IsNullOrWhiteSpace(Code);
        public string Message => Success ? ""Discount applied successfully"" : ""Discount application failed"";
        public decimal Amount => DiscountAmount;
        public decimal FinalPrice => OriginalPrice - DiscountAmount;
        public string? AudienceKey => NormalizedAudienceKey;
        public decimal? MaxDiscountAmount => EffectiveMaxDiscount;
        
        public static DiscountApplicationResult CreateSuccess(
            string code,
            {rootNamespace}.Domain.Enums.DiscountType appliedDiscountType,
            decimal appliedDiscountValue,
            decimal originalPrice,
            decimal discountAmount,
            string? normalizedAudienceKey,
            bool wasCapped,
            DateTimeOffset evaluatedAt,
            decimal? effectiveMaxDiscount)
        {{
            return new DiscountApplicationResult(
                code,
                appliedDiscountType,
                appliedDiscountValue,
                originalPrice,
                discountAmount,
                normalizedAudienceKey,
                wasCapped,
                evaluatedAt,
                effectiveMaxDiscount);
        }}
        
        public static DiscountApplicationResult CreateFailure(string message = ""Discount application failed"")
        {{
            return new DiscountApplicationResult(
                string.Empty,
                {rootNamespace}.Domain.Enums.DiscountType.Percentage,
                0m,
                0m,
                0m,
                null,
                false,
                DateTimeOffset.UtcNow,
                null);
        }}
    }}
";
                content = content.Insert(insertPosition, recordDefinition);
            }
        }
        else
        {
            // If record exists, add missing properties
            // Add FinalPrice property if not exists
            if (!content.Contains("public decimal FinalPrice", StringComparison.OrdinalIgnoreCase))
            {
                var amountPropertyPattern = @"(public\s+decimal\s+Amount\s*=>\s*DiscountAmount\s*;)";
                var amountMatch = Regex.Match(content, amountPropertyPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (amountMatch.Success)
                {
                    var insertPos = amountMatch.Index + amountMatch.Length;
                    content = content.Insert(insertPos, "\r\n        public decimal FinalPrice => OriginalPrice - DiscountAmount;");
                }
            }
            
            // Add AudienceKey property if not exists
            if (!content.Contains("public string? AudienceKey", StringComparison.OrdinalIgnoreCase))
            {
                var finalPricePattern = @"(public\s+decimal\s+FinalPrice\s*=>\s*OriginalPrice\s*-\s*DiscountAmount\s*;)";
                var finalPriceMatch = Regex.Match(content, finalPricePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (finalPriceMatch.Success)
                {
                    var insertPos = finalPriceMatch.Index + finalPriceMatch.Length;
                    content = content.Insert(insertPos, "\r\n        public string? AudienceKey => NormalizedAudienceKey;");
                }
                else
                {
                    // Try to find Amount property
                    var amountPropertyPattern = @"(public\s+decimal\s+Amount\s*=>\s*DiscountAmount\s*;)";
                    var amountMatch = Regex.Match(content, amountPropertyPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (amountMatch.Success)
                    {
                        var insertPos = amountMatch.Index + amountMatch.Length;
                        content = content.Insert(insertPos, "\r\n        public string? AudienceKey => NormalizedAudienceKey;");
                    }
                }
            }
            
            // Add MaxDiscountAmount property if not exists
            if (!content.Contains("public decimal? MaxDiscountAmount", StringComparison.OrdinalIgnoreCase))
            {
                var audienceKeyPattern = @"(public\s+string\?\s+AudienceKey\s*=>\s*NormalizedAudienceKey\s*;)";
                var audienceKeyMatch = Regex.Match(content, audienceKeyPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (audienceKeyMatch.Success)
                {
                    var insertPos = audienceKeyMatch.Index + audienceKeyMatch.Length;
                    content = content.Insert(insertPos, "\r\n        public decimal? MaxDiscountAmount => EffectiveMaxDiscount;");
                }
                else
                {
                    // Try to find FinalPrice property
                    var finalPricePattern = @"(public\s+decimal\s+FinalPrice\s*=>\s*OriginalPrice\s*-\s*DiscountAmount\s*;)";
                    var finalPriceMatch = Regex.Match(content, finalPricePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (finalPriceMatch.Success)
                    {
                        var insertPos = finalPriceMatch.Index + finalPriceMatch.Length;
                        content = content.Insert(insertPos, "\r\n        public decimal? MaxDiscountAmount => EffectiveMaxDiscount;");
                    }
                    else
                    {
                        // Try to find Amount property
                        var amountPropertyPattern = @"(public\s+decimal\s+Amount\s*=>\s*DiscountAmount\s*;)";
                        var amountMatch = Regex.Match(content, amountPropertyPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        if (amountMatch.Success)
                        {
                            var insertPos = amountMatch.Index + amountMatch.Length;
                            content = content.Insert(insertPos, "\r\n        public decimal? MaxDiscountAmount => EffectiveMaxDiscount;");
                        }
                    }
                }
            }
        }
        
        // Replace static method calls DiscountApplicationResult.Success(...) with CreateSuccess(...)
        content = Regex.Replace(
            content,
            @"DiscountApplicationResult\.Success\s*\(",
            "DiscountApplicationResult.CreateSuccess(",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Replace static method calls DiscountApplicationResult.Failure(...) with CreateFailure(...)
        content = Regex.Replace(
            content,
            @"DiscountApplicationResult\.Failure\s*\(",
            "DiscountApplicationResult.CreateFailure(",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        return content;
    }
    
    private static string FixICommandReferences(string content)
    {
        // Extract namespace from content
        var namespaceMatch = Regex.Match(content, @"namespace\s+([^;{]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var fullNamespace = "YourProject.Application.Abstractions.Messaging";
        if (namespaceMatch.Success && namespaceMatch.Groups.Count > 1)
        {
            fullNamespace = namespaceMatch.Groups[1].Value.Trim();
        }
        
        // Extract root namespace
        var rootNamespace = fullNamespace.Split('.')[0];
        
        // Ensure using statement for SharedKernel.BaseTypes is present
        var hasBaseTypes = content.Contains("SharedKernel.BaseTypes", StringComparison.OrdinalIgnoreCase);
        if (!hasBaseTypes)
        {
            // Find first using statement or namespace
            var usingPattern = @"using\s+";
            var namespacePattern = @"namespace\s+";
            var usingMatch = Regex.Match(content, usingPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var namespacePosMatch = Regex.Match(content, namespacePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            
            int insertPosition = 0;
            if (usingMatch.Success)
            {
                // Find the last using statement
                var lastUsing = usingMatch;
                while (usingMatch.Success)
                {
                    lastUsing = usingMatch;
                    usingMatch = usingMatch.NextMatch();
                }
                // Insert after the last using
                insertPosition = lastUsing.Index + lastUsing.Length;
                // Find end of line
                while (insertPosition < content.Length && content[insertPosition] != '\r' && content[insertPosition] != '\n')
                    insertPosition++;
                if (insertPosition < content.Length && content[insertPosition] == '\r')
                    insertPosition++;
                if (insertPosition < content.Length && content[insertPosition] == '\n')
                    insertPosition++;
            }
            else if (namespacePosMatch.Success)
            {
                insertPosition = namespacePosMatch.Index;
            }
            
            if (insertPosition > 0)
            {
                var usingStatement = $"using {rootNamespace}.SharedKernel.BaseTypes;\r\n";
                content = content.Insert(insertPosition, usingStatement);
            }
        }
        
        return content;
    }
    
    private static string AddDiscountDtosUsingStatement(string content)
    {
        // Extract namespace from content
        var namespaceMatch = Regex.Match(content, @"namespace\s+([^;{]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var fullNamespace = "YourProject.Application.DTOs.Discounts";
        if (namespaceMatch.Success && namespaceMatch.Groups.Count > 1)
        {
            fullNamespace = namespaceMatch.Groups[1].Value.Trim();
        }
        
        // Extract root namespace
        var rootNamespace = fullNamespace.Split('.')[0];
        
        // Check if using statement for Domain.Entities.Discounts exists
        var hasDiscountsUsing = content.Contains("Domain.Entities.Discounts", StringComparison.OrdinalIgnoreCase);
        if (!hasDiscountsUsing)
        {
            // Find first using statement or namespace
            var usingPattern = @"using\s+";
            var namespacePattern = @"namespace\s+";
            var usingMatch = Regex.Match(content, usingPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var namespacePosMatch = Regex.Match(content, namespacePattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            
            int insertPosition = 0;
            if (usingMatch.Success)
            {
                // Find the last using statement
                var lastUsing = usingMatch;
                while (usingMatch.Success)
                {
                    lastUsing = usingMatch;
                    usingMatch = usingMatch.NextMatch();
                }
                // Insert after the last using
                insertPosition = lastUsing.Index + lastUsing.Length;
                // Find end of line
                while (insertPosition < content.Length && content[insertPosition] != '\r' && content[insertPosition] != '\n')
                    insertPosition++;
                if (insertPosition < content.Length && content[insertPosition] == '\r')
                    insertPosition++;
                if (insertPosition < content.Length && content[insertPosition] == '\n')
                    insertPosition++;
            }
            else if (namespacePosMatch.Success)
            {
                insertPosition = namespacePosMatch.Index;
            }
            
            if (insertPosition > 0)
            {
                var usingStatement = $"using {rootNamespace}.Domain.Entities.Discounts;\r\n";
                content = content.Insert(insertPosition, usingStatement);
            }
        }
        
        return content;
    }
    
    private static string FixDiscountApplicationResultReferences(string content)
    {
        // Extract namespace from content
        var namespaceMatch = Regex.Match(content, @"namespace\s+([^;{]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var fullNamespace = "YourProject.Application.DTOs.Discounts";
        if (namespaceMatch.Success && namespaceMatch.Groups.Count > 1)
        {
            fullNamespace = namespaceMatch.Groups[1].Value.Trim();
        }
        
        // Extract root namespace
        var rootNamespace = fullNamespace.Split('.')[0];
        
        // Replace DiscountApplicationResult with DiscountCode.DiscountApplicationResult
        // But only if it's not already qualified and not already DiscountCode.DiscountApplicationResult
        var qualifiedName = $"{rootNamespace}.Domain.Entities.Discounts.DiscountCode.DiscountApplicationResult";
        
        // Replace unqualified DiscountApplicationResult with fully qualified name
        content = Regex.Replace(
            content,
            @"\bDiscountApplicationResult\b(?!\.)",
            qualifiedName,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        return content;
    }
    
    private static string RemoveWebSiteModelsResultReference(string content)
    {
        // Remove using statement for WebSite.Models.Result
        content = Regex.Replace(
            content,
            @"@using\s+[^\s]+\s*\.\s*WebSite\s*\.\s*Models\s*\.\s*Result\s*;?\s*\r?\n?",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Also remove any reference to WebSite.Models.Result in the file
        content = Regex.Replace(
            content,
            @"WebSite\.Models\.Result",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        return content;
    }
    
    private static string RemoveTestMenuItems(string content)
    {
        // Remove "مدیریت آزمون‌ها" menu group
        content = Regex.Replace(
            content,
            @"<div\s+class=""app-menu__group[^>]*GroupStateClass\(""Tests""\)[^>]*>.*?<div\s+class=""app-submenu[^>]*id=""testsManagementMenu[^>]*>.*?</div>\s*</div>",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );

        // Remove "آزمون ترکیبی" menu item
        content = Regex.Replace(
            content,
            @"<a\s+class=""[^""]*NavLinkClass\(""Assessment""\)[^""]*""[^>]*>.*?<span\s+class=""app-menu__title"">آزمون\s+ترکیبی[^<]+</span>\s*</a>",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );

        return content;
    }

    private static string RemoveTestRelatedReferences(string content)
    {
        // Remove using statements for Test/Assessment namespaces
        var testNamespaces = new[]
        {
            @"using\s+[^;]*\.Tests[^;]*;\s*\r?\n",
            @"using\s+[^;]*\.Commands\.Tests[^;]*;\s*\r?\n",
            @"using\s+[^;]*\.Queries\.Tests[^;]*;\s*\r?\n",
            @"using\s+[^;]*\.Assessments[^;]*;\s*\r?\n",
            @"using\s+[^;]*\.Entities\.Tests[^;]*;\s*\r?\n",
            @"using\s+[^;]*\.Entities\.Assessments[^;]*;\s*\r?\n"
        };

        foreach (var pattern in testNamespaces)
        {
            content = Regex.Replace(
                content,
                pattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }

        // Remove references to Test/Assessment query/command types and DTOs
        var testTypes = new[]
        {
            "GetPublicTestListQuery",
            "GetTestByIdQuery",
            "StartTestAttemptCommand",
            "GetUserTestAttemptQuery",
            "SubmitTestAnswerCommand",
            "CompleteTestAttemptCommand",
            "GetUserTestAttemptsQuery",
            "AssessmentResponse",
            "ScoringOptions",
            "AssessmentRun",
            "OrganizationStatus",
            // Test/Assessment DTOs
            "TestQuestionDto",
            "UserTestAttemptDetailDto",
            "UserTestAttemptDto",
            "TestListDto",
            "TestDetailDto",
            "TestResultDto"
        };

        foreach (var typeName in testTypes)
        {
            // Remove field declarations with these types
            content = Regex.Replace(
                content,
                $@"private\s+(readonly\s+)?IQueryHandler<{Regex.Escape(typeName)}[^>]*>\s+_[a-zA-Z0-9_]+\s*;\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            content = Regex.Replace(
                content,
                $@"private\s+(readonly\s+)?ICommandHandler<{Regex.Escape(typeName)}[^>]*>\s+_[a-zA-Z0-9_]+\s*;\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            content = Regex.Replace(
                content,
                $@"private\s+(readonly\s+)?{Regex.Escape(typeName)}\s+_[a-zA-Z0-9_]+\s*;\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            
            // Remove constructor parameters with these types (more careful pattern)
            // Pattern: TypeName variableName, (with optional whitespace)
            content = Regex.Replace(
                content,
                $@"{Regex.Escape(typeName)}\s+[a-zA-Z0-9_]+\s*,?\s*(?=\r?\n)",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            
            // Remove IQueryHandler<TypeName> and ICommandHandler<TypeName> parameters
            content = Regex.Replace(
                content,
                $@"IQueryHandler<{Regex.Escape(typeName)}[^>]*>\s+[a-zA-Z0-9_]+\s*,?\s*(?=\r?\n)",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            content = Regex.Replace(
                content,
                $@"ICommandHandler<{Regex.Escape(typeName)}[^>]*>\s+[a-zA-Z0-9_]+\s*,?\s*(?=\r?\n)",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            
            // Remove variable declarations
            content = Regex.Replace(
                content,
                $@"var\s+[a-zA-Z0-9_]+\s*=\s*[^;]*{Regex.Escape(typeName)}[^;]*;\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
            );
            
            // Remove property declarations with these types
            content = Regex.Replace(
                content,
                $@"public\s+{Regex.Escape(typeName)}\s+[a-zA-Z0-9_]+\s*{{\s*get\s*;\s*set\s*;\s*}}\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            content = Regex.Replace(
                content,
                $@"public\s+{Regex.Escape(typeName)}\?\s+[a-zA-Z0-9_]+\s*{{\s*get\s*;\s*set\s*;\s*}}\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            
            // Remove return type declarations
            content = Regex.Replace(
                content,
                $@"\s*:\s*{Regex.Escape(typeName)}\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            content = Regex.Replace(
                content,
                $@"\s*:\s*{Regex.Escape(typeName)}\?\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            
            // Remove method return types
            content = Regex.Replace(
                content,
                $@"Task<{Regex.Escape(typeName)}>\s+",
                "Task ",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            content = Regex.Replace(
                content,
                $@"Task<{Regex.Escape(typeName)}\?>\s+",
                "Task ",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            content = Regex.Replace(
                content,
                $@"{Regex.Escape(typeName)}\s+[a-zA-Z0-9_]+\s*\(",
                "void ",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            content = Regex.Replace(
                content,
                $@"{Regex.Escape(typeName)}\?\s+[a-zA-Z0-9_]+\s*\(",
                "void ",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }

        // Remove references to Tests namespace in using statements
        content = Regex.Replace(
            content,
            @"using\s+[^;]*\.Tests\s*;\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        // Remove OrganizationStatus enum references
        content = Regex.Replace(
            content,
            @"OrganizationStatus\.[a-zA-Z0-9_]+\s*",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        return content;
    }
    
    private static string RemoveLearningMetricsReferences(string content)
    {
        // Remove LearningMetricsDto type references and variable declarations
        content = Regex.Replace(
            content,
            @"LearningMetricsDto\s+[a-zA-Z0-9_]+\s*[=;]?[^\r\n]*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Remove LearningMetricsDto variable declarations with await
        content = Regex.Replace(
            content,
            @"var\s+[a-zA-Z0-9_]+\s*=\s*await\s+BuildLearningMetricsAsync[^;]+;\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        // Fix SystemPerformanceSummaryDto constructor calls - remove learning argument
        // Pattern: new SystemPerformanceSummaryDto(period, people, commerce, learning, content, now)
        // Should become: new SystemPerformanceSummaryDto(period, people, commerce, content, now)
        content = Regex.Replace(
            content,
            @"new\s+SystemPerformanceSummaryDto\s*\(\s*([^,]+),\s*([^,]+),\s*([^,]+),\s*[^,]+,\s*([^,]+),\s*([^)]+)\)",
            "new SystemPerformanceSummaryDto($1, $2, $3, $4, $5)",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        // Remove LearningMetricsViewModel constructor calls that reference Learning
        // Find and replace LearningMetricsViewModel initialization that uses .Learning properties
        content = Regex.Replace(
            content,
            @"new\s+LearningMetricsViewModel\s*\(\s*[^)]*\.Learning\.[^)]+\)",
            "new LearningMetricsViewModel(0, 0, 0, 0, 0, 0, 0, 0, 0, 0)",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        // Remove lines that assign .Learning properties to variables
        // Pattern: var something = summary.Learning.Something;
        content = Regex.Replace(
            content,
            @"^\s*var\s+[a-zA-Z0-9_]+\s*=\s*[a-zA-Z0-9_]+\.Learning\.[a-zA-Z0-9_]+\s*;\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Remove lines that assign .Learning properties (without var)
        // Pattern: something = summary.Learning.Something;
        content = Regex.Replace(
            content,
            @"^\s*[a-zA-Z0-9_]+\s*=\s*[a-zA-Z0-9_]+\.Learning\.[a-zA-Z0-9_]+\s*;\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Remove .Learning property access in expressions (replace with placeholder)
        content = Regex.Replace(
            content,
            @"\.Learning\.[a-zA-Z0-9_]+",
            ".LearningPlaceholder",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        return content;
    }
    
    private static string RemoveTestResultReferences(string content)
    {
        // Remove testResult variable references
        content = Regex.Replace(
            content,
            @"var\s+testResult\s*=[^;]+;\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        // Remove lines that use testResult
        content = Regex.Replace(
            content,
            @"[^\r\n]*testResult[^\r\n]*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        return content;
    }
    
    private static string RemoveInvoiceTestProperties(string content)
    {
        // Remove TestAttemptId and TestAttemptStatus from InvoiceDetailDto
        content = Regex.Replace(
            content,
            @"@Model\.TestAttemptId[^\r\n]*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        content = Regex.Replace(
            content,
            @"@Model\.TestAttemptStatus[^\r\n]*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        // Remove entire lines that reference TestAttempt
        content = Regex.Replace(
            content,
            @"[^\r\n]*TestAttempt[^\r\n]*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        
        return content;
    }
    
    private static string FixDashboardControllerBuildEmptySummary(string content)
    {
        // Fix PeriodWindowDto constructor in BuildEmptySummary - add previousPeriodEnd parameter
        // Pattern: new PeriodWindowDto(currentPeriodStart, currentPeriodEnd, previousPeriodStart, currentWeekStart, previousWeekStart)
        // Should become: new PeriodWindowDto(currentPeriodStart, currentPeriodEnd, previousPeriodStart, previousPeriodEnd, currentWeekStart, previousWeekStart)
        content = Regex.Replace(
            content,
            @"new\s+PeriodWindowDto\s*\(\s*currentPeriodStart\s*,\s*currentPeriodEnd\s*,\s*previousPeriodStart\s*,\s*currentWeekStart\s*,\s*previousWeekStart\s*\)",
            "new PeriodWindowDto(\r\n                currentPeriodStart,\r\n                currentPeriodEnd,\r\n                previousPeriodStart,\r\n                previousPeriodEnd,\r\n                currentWeekStart,\r\n                previousWeekStart)",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        // Remove LearningMetricsDto from BuildEmptySummary constructor call
        // Pattern: new SystemPerformanceSummaryDto(..., new LearningMetricsDto(...), ...)
        content = Regex.Replace(
            content,
            @",\s*new\s+LearningMetricsDto\s*\([^)]+\)\s*,",
            ",",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        // Fix SystemPerformanceSummaryDto constructor call - ensure proper formatting
        // Pattern: new SystemPerformanceSummaryDto(new PeriodWindowDto(...), new PeopleMetricsDto(...), new CommerceMetricsDto(...), new LearningMetricsDto(...), new ContentMetricsDto(...), referenceTime)
        // After fix: new SystemPerformanceSummaryDto(new PeriodWindowDto(...), new PeopleMetricsDto(...), new CommerceMetricsDto(...), new ContentMetricsDto(...), referenceTime)
        // Make sure PeriodWindowDto has all 6 parameters
        content = Regex.Replace(
            content,
            @"return\s+new\s+SystemPerformanceSummaryDto\s*\(\s*new\s+PeriodWindowDto\s*\([^)]+\)\s*,\s*new\s+PeopleMetricsDto\([^)]+\)\s*,\s*new\s+CommerceMetricsDto\([^)]+\)\s*,\s*new\s+LearningMetricsDto\([^)]+\)\s*,\s*new\s+ContentMetricsDto\([^)]+\)\s*,\s*([^)]+)\)\s*;",
            "return new SystemPerformanceSummaryDto(\r\n            new PeriodWindowDto(\r\n                currentPeriodStart,\r\n                currentPeriodEnd,\r\n                previousPeriodStart,\r\n                previousPeriodEnd,\r\n                currentWeekStart,\r\n                previousWeekStart),\r\n            new PeopleMetricsDto(0, 0, 0, 0, 0, 0, 0, 0),\r\n            new CommerceMetricsDto(0m, 0m, 0m, 0m, 0m, 0, 0, 0, 0, 0),\r\n            new ContentMetricsDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),\r\n            $1);",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        return content;
    }
    
    private static string RemoveCheckoutControllerTestMethods(string content)
    {
        // Remove Test method (HttpGet)
        // Match from [HttpGet] to the closing brace of the method
        var testMethodPattern = @"\[HttpGet\]\s*\r?\n\s*public\s+async\s+Task<IActionResult>\s+Test\s*\([^)]+\)\s*\{";
        var testMethodMatch = Regex.Match(content, testMethodPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        while (testMethodMatch.Success)
        {
            var startIndex = testMethodMatch.Index;
            var braceStart = content.IndexOf('{', startIndex);
            if (braceStart >= 0)
            {
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                
                if (braceCount == 0)
                {
                    // Find the start of the line
                    var lineStart = startIndex;
                    while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                        lineStart--;
                    
                    content = content.Remove(lineStart, i - lineStart);
                    testMethodMatch = Regex.Match(content, testMethodPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
        
        // Remove PayTest method (HttpPost)
        var payTestMethodPattern = @"\[HttpPost\]\s*\r?\n\s*\[ValidateAntiForgeryToken\]\s*\r?\n\s*public\s+async\s+Task<IActionResult>\s+PayTest\s*\([^)]+\)\s*\{";
        var payTestMethodMatch = Regex.Match(content, payTestMethodPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        while (payTestMethodMatch.Success)
        {
            var startIndex = payTestMethodMatch.Index;
            var braceStart = content.IndexOf('{', startIndex);
            if (braceStart >= 0)
            {
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                
                if (braceCount == 0)
                {
                    var lineStart = startIndex;
                    while (lineStart > 0 && content[lineStart - 1] != '\n' && content[lineStart - 1] != '\r')
                        lineStart--;
                    
                    content = content.Remove(lineStart, i - lineStart);
                    payTestMethodMatch = Regex.Match(content, payTestMethodPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
        
        // Remove GetTestTypeName method
        content = Regex.Replace(
            content,
            @"private\s+static\s+string\s+GetTestTypeName\s*\([^)]+\)\s*=>\s*type\s+switch\s*\{[^}]+\}\s*;",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        // Remove using aa.Domain.Enums if TestType was the only usage
        // This is a simple check - might need refinement
        if (!content.Contains("InvoiceItemType") && !content.Contains("InvoiceStatus") && !content.Contains("TransactionStatus"))
        {
            content = Regex.Replace(
                content,
                @"using\s+[^;]+\.Domain\.Enums\s*;\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }
        
        return content;
    }
    
    private static string CleanInvoiceDetailsView(string content)
    {
        // Add required using statements if not present
        var hasTextJson = content.Contains("@using System.Text.Json", StringComparison.OrdinalIgnoreCase);
        var hasGlobalization = content.Contains("@using System.Globalization", StringComparison.OrdinalIgnoreCase);
        
        if (!hasTextJson || !hasGlobalization)
        {
            // Try to find the best insertion point: after existing @using statements or before @model
            var insertPosition = 0;
            var usingStatements = new List<string>();
            
            // First, try to find existing @using statements and add after them
            var existingUsingPattern = @"(@using\s+[^\r\n]+[\r\n]+)";
            var existingUsingMatches = Regex.Matches(content, existingUsingPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            
            if (existingUsingMatches.Count > 0)
            {
                // Insert after the last @using statement
                var lastUsing = existingUsingMatches[existingUsingMatches.Count - 1];
                insertPosition = lastUsing.Index + lastUsing.Length;
            }
            else
            {
                // If no @using found, try to find @model and insert before it
                var modelPattern = @"@model\s+";
                var modelMatch = Regex.Match(content, modelPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (modelMatch.Success)
                {
                    insertPosition = modelMatch.Index;
                }
                else
                {
                    // If no @model found, insert at the beginning
                    insertPosition = 0;
                }
            }
            
            if (!hasTextJson)
            {
                usingStatements.Add("@using System.Text.Json");
            }
            
            if (!hasGlobalization)
            {
                usingStatements.Add("@using System.Globalization");
            }
            
            if (usingStatements.Count > 0)
            {
                var usingBlock = string.Join("\r\n", usingStatements) + "\r\n";
                content = content.Insert(insertPosition, usingBlock);
            }
        }
        
        // Remove testItem and isPaid variable declarations
        content = Regex.Replace(
            content,
            @"var\s+testItem\s*=[^;]+;\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        content = Regex.Replace(
            content,
            @"var\s+isPaid\s*=[^;]+;\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        
        // Remove entire @if block for testItem
        // Match from @if (testItem != null && isPaid) to the matching @else if or @}
        var testItemIfPattern = @"@if\s*\(\s*testItem\s*!=\s*null\s*&&\s*isPaid\s*\)";
        var testItemIfMatch = Regex.Match(content, testItemIfPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        while (testItemIfMatch.Success)
        {
            var startIndex = testItemIfMatch.Index;
            var braceStart = content.IndexOf('{', startIndex);
            if (braceStart >= 0)
            {
                int braceCount = 1;
                int i = braceStart + 1;
                while (i < content.Length && braceCount > 0)
                {
                    if (content[i] == '{') braceCount++;
                    else if (content[i] == '}') braceCount--;
                    i++;
                }
                
                if (braceCount == 0)
                {
                    // Check if there's an else if after
                    var afterBlock = content.Substring(i);
                    var elseIfMatch = Regex.Match(afterBlock, @"^\s*else\s+if", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (elseIfMatch.Success)
                    {
                        // Remove the entire if block including the else if, but keep the else if content
                        var elseIfStart = i + elseIfMatch.Index;
                        var elseIfBraceStart = content.IndexOf('{', elseIfStart);
                        if (elseIfBraceStart >= 0)
                        {
                            int elseIfBraceCount = 1;
                            int j = elseIfBraceStart + 1;
                            while (j < content.Length && elseIfBraceCount > 0)
                            {
                                if (content[j] == '{') elseIfBraceCount++;
                                else if (content[j] == '}') elseIfBraceCount--;
                                j++;
                            }
                            
                            if (elseIfBraceCount == 0)
                            {
                                // Replace the entire if-else if with just the else if content
                                var elseIfContent = content.Substring(elseIfBraceStart + 1, j - 1 - elseIfBraceStart - 1);
                                content = content.Remove(startIndex, j - startIndex);
                                content = content.Insert(startIndex, elseIfContent);
                            }
                        }
                    }
                    else
                    {
                        // Just remove the if block
                        content = content.Remove(startIndex, i - startIndex);
                    }
                    
                    testItemIfMatch = Regex.Match(content, testItemIfPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
        
        // Remove using aa.Domain.Enums if it's no longer needed
        if (!content.Contains("InvoiceItemType") && !content.Contains("InvoiceStatus"))
        {
            content = Regex.Replace(
                content,
                @"@using\s+[^;]+\.Domain\.Enums\s*\r?\n",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
        }
        
        return content;
    }

}


