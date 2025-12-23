using ProjectGenerator.Core.Models;
using ProjectGenerator.Core.Utilities;
using System.Text;

namespace ProjectGenerator.Core.Generators;

public class SolutionGenerator
{
    private readonly ProjectConfig _config;
    private readonly string _solutionPath;

    public SolutionGenerator(ProjectConfig config)
    {
        _config = config;
        _solutionPath = Path.Combine(_config.OutputPath, $"{_config.ProjectName}.sln");
    }

    public void Generate()
    {
        Console.WriteLine($"Creating solution structure at: {_config.OutputPath}");
        
        // Create output directory
        Directory.CreateDirectory(_config.OutputPath);

        var clonedReference = ReferenceSolutionCloner.TryClone(_config);

        if (!clonedReference)
        {
            // Generate all layers
            GenerateLayers();
        }
        else if (_config.Options.IncludeTests)
        {
            var layerGenerator = new LayerGenerator(_config);
            layerGenerator.GenerateLayer("UnitTests", LayerType.Tests);
        }

        // Generate solution file
        GenerateSolutionFile();

        Console.WriteLine("✓ Solution structure created successfully!");
    }

    private void GenerateLayers()
    {
        var layerGenerator = new LayerGenerator(_config);

        // Core layers
        layerGenerator.GenerateLayer("Domain", LayerType.Domain);
        layerGenerator.GenerateLayer("SharedKernel", LayerType.SharedKernel);
        layerGenerator.GenerateLayer("Application", LayerType.Application);
        layerGenerator.GenerateLayer("Infrastructure", LayerType.Infrastructure);

        // Presentation layer
        if (_config.Options.IncludeWebSite)
        {
            layerGenerator.GenerateWebSiteLayer();
        }

        // Test layer
        if (_config.Options.IncludeTests)
        {
            layerGenerator.GenerateLayer("UnitTests", LayerType.Tests);
        }

        // Generate seed data if requested
        if (_config.Options.GenerateInitialSeedData)
        {
            var seedGenerator = new SeedDataGenerator(_config);
            seedGenerator.Generate();
        }
    }

    private void GenerateSolutionFile()
    {
        var sb = new StringBuilder();
        var projectGuids = new Dictionary<string, string>();

        var root = _config.OutputPath;
        var srcRoot = Path.Combine(root, "src");
        var testsRoot = Path.Combine(root, "tests");
        
        sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        sb.AppendLine("# Visual Studio Version 17");
        sb.AppendLine("VisualStudioVersion = 17.0.31903.59");
        sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

        // Add WebSite project FIRST to make it the startup project
        if (_config.Options.IncludeWebSite)
        {
            var webProjectPath = ResolveProjectPath(
                root,
                // preferred path (matches generator/cloner output)
                Path.Combine(srcRoot, $"{_config.ProjectName}.WebSite", $"{_config.ProjectName}.WebSite.csproj"),
                // fallback: any *.WebSite.csproj under src
                FindFirstCsproj(srcRoot, p => p.EndsWith(".WebSite.csproj", StringComparison.OrdinalIgnoreCase))
            );

            var webDisplayName = $"{_config.ProjectName}.WebSite";
            var websiteGuid = AddProject(sb, webDisplayName, webProjectPath);
            projectGuids.Add(webDisplayName, websiteGuid);
        }

        // Core projects
        projectGuids.Add("Domain", AddProject(sb, "Domain", ResolveProjectPath(
            root,
            Path.Combine(srcRoot, "Domain", "Domain.csproj"),
            FindFirstCsproj(Path.Combine(srcRoot, "Domain"), _ => true),
            FindFirstCsproj(srcRoot, p => p.EndsWith("\\Domain\\Domain.csproj", StringComparison.OrdinalIgnoreCase) || p.EndsWith("/Domain/Domain.csproj", StringComparison.OrdinalIgnoreCase))
        )));

        projectGuids.Add("SharedKernel", AddProject(sb, "SharedKernel", ResolveProjectPath(
            root,
            Path.Combine(srcRoot, "SharedKernel", "SharedKernel.csproj"),
            FindFirstCsproj(Path.Combine(srcRoot, "SharedKernel"), _ => true)
        )));

        projectGuids.Add("Application", AddProject(sb, "Application", ResolveProjectPath(
            root,
            Path.Combine(srcRoot, "Application", "Application.csproj"),
            FindFirstCsproj(Path.Combine(srcRoot, "Application"), _ => true)
        )));

        projectGuids.Add("Infrastructure", AddProject(sb, "Infrastructure", ResolveProjectPath(
            root,
            Path.Combine(srcRoot, "Infrastructure", "Infrastructure.csproj"),
            FindFirstCsproj(Path.Combine(srcRoot, "Infrastructure"), _ => true)
        )));

        if (_config.Options.IncludeTests)
        {
            projectGuids.Add("UnitTests", AddProject(sb, "UnitTests", ResolveProjectPath(
                root,
                Path.Combine(testsRoot, "UnitTests", "UnitTests.csproj"),
                FindFirstCsproj(Path.Combine(testsRoot, "UnitTests"), _ => true),
                FindFirstCsproj(testsRoot, p => p.EndsWith("\\UnitTests\\UnitTests.csproj", StringComparison.OrdinalIgnoreCase) || p.EndsWith("/UnitTests/UnitTests.csproj", StringComparison.OrdinalIgnoreCase))
            )));
        }

        // Add solution folders
        var srcFolderGuid = Guid.NewGuid().ToString().ToUpper();
        var testsFolderGuid = Guid.NewGuid().ToString().ToUpper();
        
        sb.AppendLine($"Project(\"{{2150E333-8FDC-42A3-9474-1A3956D46DE8}}\") = \"src\", \"src\", \"{{{srcFolderGuid}}}\"");
        sb.AppendLine("EndProject");
        
        if (_config.Options.IncludeTests)
        {
            sb.AppendLine($"Project(\"{{2150E333-8FDC-42A3-9474-1A3956D46DE8}}\") = \"tests\", \"tests\", \"{{{testsFolderGuid}}}\"");
            sb.AppendLine("EndProject");
        }

        // Global section
        sb.AppendLine("Global");
        
        // Solution configurations
        sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        sb.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
        sb.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
        sb.AppendLine("\tEndGlobalSection");
        
        // Project configurations
        sb.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
        foreach (var project in projectGuids)
        {
            sb.AppendLine($"\t\t{{{project.Value}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            sb.AppendLine($"\t\t{{{project.Value}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
            sb.AppendLine($"\t\t{{{project.Value}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
            sb.AppendLine($"\t\t{{{project.Value}}}.Release|Any CPU.Build.0 = Release|Any CPU");
        }
        sb.AppendLine("\tEndGlobalSection");
        
        // Nested projects (organize in folders)
        sb.AppendLine("\tGlobalSection(NestedProjects) = preSolution");
        if (_config.Options.IncludeWebSite)
        {
            sb.AppendLine($"\t\t{{{projectGuids[$"{_config.ProjectName}.WebSite"]}}} = {{{srcFolderGuid}}}");
        }
        sb.AppendLine($"\t\t{{{projectGuids["Domain"]}}} = {{{srcFolderGuid}}}");
        sb.AppendLine($"\t\t{{{projectGuids["SharedKernel"]}}} = {{{srcFolderGuid}}}");
        sb.AppendLine($"\t\t{{{projectGuids["Application"]}}} = {{{srcFolderGuid}}}");
        sb.AppendLine($"\t\t{{{projectGuids["Infrastructure"]}}} = {{{srcFolderGuid}}}");
        if (_config.Options.IncludeTests)
        {
            sb.AppendLine($"\t\t{{{projectGuids["UnitTests"]}}} = {{{testsFolderGuid}}}");
        }
        sb.AppendLine("\tEndGlobalSection");
        
        sb.AppendLine("EndGlobal");

        File.WriteAllText(_solutionPath, sb.ToString());
        Console.WriteLine($"✓ Solution file created: {_solutionPath}");
        
        if (_config.Options.IncludeWebSite)
        {
            Console.WriteLine($"✓ {_config.ProjectName}.WebSite set as startup project");
        }
    }

    private static string ResolveProjectPath(string solutionRoot, params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (File.Exists(candidate))
            {
                // Ensure we emit a relative path in the .sln (VS expects relative paths)
                return Path.GetRelativePath(solutionRoot, candidate);
            }
        }

        // Last resort: don't fail project generation; emit the first candidate as a relative path (even if missing)
        // so the user at least gets a .sln and we can see which project didn't resolve.
        var fallback = candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
        if (!string.IsNullOrWhiteSpace(fallback))
        {
            Console.WriteLine($"⚠ Could not find .csproj on disk. Using fallback path in solution: {fallback}");
            return Path.GetRelativePath(solutionRoot, fallback);
        }

        Console.WriteLine("⚠ Could not resolve a valid .csproj path for solution generation (no candidates provided).");
        return "MISSING_PROJECT.csproj";
    }

    private static string? FindFirstCsproj(string? rootDir, Func<string, bool> predicate)
    {
        if (string.IsNullOrWhiteSpace(rootDir) || !Directory.Exists(rootDir))
        {
            return null;
        }

        return Directory.EnumerateFiles(rootDir, "*.csproj", SearchOption.AllDirectories)
            .FirstOrDefault(predicate);
    }

    private string AddProject(StringBuilder sb, string projectName, string projectPath)
    {
        var projectGuid = Guid.NewGuid().ToString().ToUpper();

        sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projectName}\", \"{projectPath}\", \"{{{projectGuid}}}\"");
        sb.AppendLine("EndProject");
        
        return projectGuid;
    }
}

