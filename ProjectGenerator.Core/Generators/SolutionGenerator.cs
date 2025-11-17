using ProjectGenerator.Models;
using System.Text;

namespace ProjectGenerator.Generators;

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

        // Generate all layers
        GenerateLayers();

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
        
        sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        sb.AppendLine("# Visual Studio Version 17");
        sb.AppendLine("VisualStudioVersion = 17.0.31903.59");
        sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

        // Add projects
        AddProject(sb, "Domain", "src");
        AddProject(sb, "SharedKernel", "src");
        AddProject(sb, "Application", "src");
        AddProject(sb, "Infrastructure", "src");

        if (_config.Options.IncludeWebSite)
        {
            AddProject(sb, $"{_config.ProjectName}.WebSite", "src");
        }

        if (_config.Options.IncludeTests)
        {
            AddProject(sb, "UnitTests", "tests");
        }

        sb.AppendLine("Global");
        sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        sb.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
        sb.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("EndGlobal");

        File.WriteAllText(_solutionPath, sb.ToString());
        Console.WriteLine($"✓ Solution file created: {_solutionPath}");
    }

    private void AddProject(StringBuilder sb, string projectName, string folder)
    {
        var projectGuid = Guid.NewGuid().ToString().ToUpper();
        var projectPath = string.IsNullOrEmpty(folder)
            ? $"{projectName}\\{projectName}.csproj"
            : $"{folder}\\{projectName}\\{projectName}.csproj";

        sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projectName}\", \"{projectPath}\", \"{{{projectGuid}}}\"");
        sb.AppendLine("EndProject");
    }
}
