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
        
        sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        sb.AppendLine("# Visual Studio Version 17");
        sb.AppendLine("VisualStudioVersion = 17.0.31903.59");
        sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

        // Add WebSite project FIRST to make it the startup project
        if (_config.Options.IncludeWebSite)
        {
            var websiteGuid = AddProject(sb, $"{_config.ProjectName}.WebSite", "src");
            projectGuids.Add($"{_config.ProjectName}.WebSite", websiteGuid);
        }

        // Add other projects
        projectGuids.Add("Domain", AddProject(sb, "Domain", "src"));
        projectGuids.Add("SharedKernel", AddProject(sb, "SharedKernel", "src"));
        projectGuids.Add("Application", AddProject(sb, "Application", "src"));
        projectGuids.Add("Infrastructure", AddProject(sb, "Infrastructure", "src"));

        if (_config.Options.IncludeTests)
        {
            projectGuids.Add("UnitTests", AddProject(sb, "UnitTests", "tests"));
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

    private string AddProject(StringBuilder sb, string projectName, string folder)
    {
        var projectGuid = Guid.NewGuid().ToString().ToUpper();
        var projectPath = string.IsNullOrEmpty(folder)
            ? $"{projectName}\\{projectName}.csproj"
            : $"{folder}\\{projectName}\\{projectName}.csproj";

        sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projectName}\", \"{projectPath}\", \"{{{projectGuid}}}\"");
        sb.AppendLine("EndProject");
        
        return projectGuid;
    }
}

