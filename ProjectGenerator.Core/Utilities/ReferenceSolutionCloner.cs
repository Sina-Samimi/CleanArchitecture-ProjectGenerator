using ProjectGenerator.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectGenerator.Core.Utilities;

internal static class ReferenceSolutionCloner
{
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
        "logs",
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
        "Areas/Admin/Views/Assessment",
        "Areas/Admin/Views/Tests",
        "Areas/Admin/Views/Organizations",
        "Views/Test",
        "Controllers/TestController.cs",
        "Controllers/AssessmentController.cs",
        "Areas/User/Controllers/TestController.cs",
        "Areas/User/Controllers/AssessmentController.cs",
        "Areas/User/Views/Test",
        "Areas/User/Views/Assessment",
        // Specific files
        "Question.cs",
        "UserResponse.cs",
        "Talent.cs",
        "TalentScore.cs",
        "Organization.cs"
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
            Console.WriteLine("⚠ Reference EndPoint.WebSite folder not found. Falling back to template generation.");
            return false;
        }

        var referenceRoot = Directory.GetParent(websiteSource)?.FullName;
        if (referenceRoot is null)
        {
            Console.WriteLine("⚠ Unable to resolve reference root folder.");
            return false;
        }

        var referenceSrcRoot = Path.Combine(referenceRoot, "src");
        if (!Directory.Exists(referenceSrcRoot))
        {
            Console.WriteLine("⚠ Reference src folder not found. Falling back to template generation.");
            return false;
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
            CopyDirectory(sourceDir, targetDir, config);
        }

        var websiteTargetDir = Path.Combine(targetSrcRoot, $"{config.ProjectName}.WebSite");
        Console.WriteLine($" - {config.ProjectName}.WebSite");
        CopyDirectory(websiteSource, websiteTargetDir, config);

        Console.WriteLine("✓ Reference source cloned successfully");
        return true;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir, ProjectConfig config)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var directory in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            if (ShouldSkip(directory, sourceDir))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(sourceDir, directory);
            var transformedRelative = TransformRelativePath(relativePath, config);
            Directory.CreateDirectory(Path.Combine(destinationDir, transformedRelative));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            if (ShouldSkip(file, sourceDir))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(sourceDir, file);
            var transformedRelative = TransformRelativePath(relativePath, config);
            var destinationFile = Path.Combine(destinationDir, transformedRelative);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

            if (IsTextFile(file))
            {
                var content = File.ReadAllText(file, Encoding.UTF8);
                var fileName = Path.GetFileName(file);
                var transformedContent = ApplyContentTransformations(content, config, fileName);
                File.WriteAllText(destinationFile, transformedContent, Encoding.UTF8);
            }
            else
            {
                File.Copy(file, destinationFile, overwrite: true);
            }
        }
    }

    private static bool ShouldSkip(string path, string basePath)
    {
        var relative = Path.GetRelativePath(basePath, path);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        // Skip standard build/version control directories
        if (segments.Any(segment => SkippedDirectories.Contains(segment)))
        {
            return true;
        }

        // Skip test/assessment related paths
        if (IsTestRelatedPath(relative))
        {
            return true;
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
                "UserTestAttemptConfiguration.cs"
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

    private static string TransformRelativePath(string relativePath, ProjectConfig config)
    {
        return relativePath.Replace("EndPoint.WebSite", $"{config.ProjectName}.WebSite", StringComparison.OrdinalIgnoreCase);
    }

    private static string ApplyContentTransformations(string content, ProjectConfig config, string? fileName = null)
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
        }

        // Special handling for AppDbContext to remove Organization references
        if (fileName != null && fileName.Equals("AppDbContext.cs", StringComparison.OrdinalIgnoreCase))
        {
            transformed = RemoveOrganizationFromDbContext(transformed);
        }

        // Special handling for SeedData to remove Talent/Question references
        if (fileName != null && fileName.Equals("SeedData.cs", StringComparison.OrdinalIgnoreCase))
        {
            transformed = RemoveTestEntitiesFromSeedData(transformed);
        }

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
        systemUserStartMatch = Regex.Match(content, systemUserStartPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (systemUserStartMatch.Success)
        {
            var startIndex = systemUserStartMatch.Index;
            var declarationEnd = startIndex + systemUserStartMatch.Length;
            
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
        content = Regex.Replace(
            content,
            @"^\s*\);\s*\r?\n",
            string.Empty,
            RegexOptions.Multiline
        );
        content = Regex.Replace(
            content,
            @"builder\.Services\.AddSingleton<[^>]*MatrixLoader[^>]*>\([^}]+}\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
        );
        content = Regex.Replace(
            content,
            @"builder\.Services\.AddScoped<IQuestionImporter[^>]*>\([^)]+\);\s*\r?\n",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );
        content = Regex.Replace(
            content,
            @"builder\.Services\.AddScoped<[^>]*AssessmentService[^>]*>\([^)]+\);\s*\r?\n",
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

        // Remove references to Test/Assessment query/command types
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
            "OrganizationStatus"
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

}


