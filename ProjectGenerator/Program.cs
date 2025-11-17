using ProjectGenerator.Core.Models;
using ProjectGenerator.Core.Generators;
using Newtonsoft.Json;

namespace ProjectGenerator;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════╗");
        Console.WriteLine("║   Clean Architecture Project Generator        ║");
        Console.WriteLine("╚════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            var config = GetConfiguration(args);
            
            if (config == null)
            {
                ShowUsage();
                return;
            }

            ValidateConfiguration(config);
            
            var generator = new SolutionGenerator(config);
            generator.Generate();

            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║  ✓ Project created successfully!               ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine($"Location: {config.OutputPath}");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("  1. Navigate to the project directory");
            Console.WriteLine("  2. Run: dotnet restore");
            Console.WriteLine("  3. Run: dotnet build");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    static ProjectConfig? GetConfiguration(string[] args)
    {
        // Check if using config file
        if (args.Length > 0 && args[0] == "--config" && args.Length > 1)
        {
            return LoadConfigFromFile(args[1]);
        }

        // Interactive mode
        if (args.Length == 0 || args[0] == "--interactive")
        {
            return GetInteractiveConfiguration();
        }

        // Command line arguments
        return ParseCommandLineArguments(args);
    }

    static ProjectConfig LoadConfigFromFile(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var json = File.ReadAllText(configPath);
        var config = JsonConvert.DeserializeObject<ProjectConfig>(json);
        
        if (config == null)
        {
            throw new InvalidOperationException("Invalid configuration file");
        }

        return config;
    }

    static ProjectConfig GetInteractiveConfiguration()
    {
        var config = new ProjectConfig();

        Console.Write("Enter project name: ");
        config.ProjectName = Console.ReadLine()?.Trim() ?? "";

        Console.Write("Enter output path (default: current directory): ");
        var outputPath = Console.ReadLine()?.Trim();
        config.OutputPath = string.IsNullOrEmpty(outputPath) 
            ? Path.Combine(Directory.GetCurrentDirectory(), config.ProjectName)
            : Path.Combine(outputPath, config.ProjectName);

        Console.Write("Enter namespace (default: same as project name): ");
        var ns = Console.ReadLine()?.Trim();
        config.Namespace = string.IsNullOrEmpty(ns) ? config.ProjectName : ns;

        Console.Write("Include WebSite project? (Y/n): ");
        var includeWeb = Console.ReadLine()?.Trim().ToLower();
        config.Options.IncludeWebSite = includeWeb != "n";

        Console.Write("Include Tests project? (Y/n): ");
        var includeTests = Console.ReadLine()?.Trim().ToLower();
        config.Options.IncludeTests = includeTests != "n";

        Console.Write("Generate seed data (users/roles)? (y/N): ");
        var generateSeed = Console.ReadLine()?.Trim().ToLower();
        config.Options.GenerateInitialSeedData = generateSeed == "y";

        if (config.Options.IncludeWebSite)
        {
            ConfigureTheme(config);
        }

        if (config.Options.GenerateInitialSeedData)
        {
            ConfigureSeedData(config);
        }

        return config;
    }

    static void ConfigureTheme(ProjectConfig config)
    {
        Console.WriteLine("\n--- Configuring Theme Settings ---");
        
        Console.Write($"Site name (default: {config.Theme.SiteName}): ");
        var siteName = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(siteName))
        {
            config.Theme.SiteName = siteName;
        }

        Console.Write($"Primary color (default: {config.Theme.PrimaryColor}): ");
        var primaryColor = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(primaryColor))
        {
            config.Theme.PrimaryColor = primaryColor;
        }

        Console.Write($"Secondary color (default: {config.Theme.SecondaryColor}): ");
        var secondaryColor = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(secondaryColor))
        {
            config.Theme.SecondaryColor = secondaryColor;
        }

        Console.Write($"Font family (default: {config.Theme.FontFamily}): ");
        var fontFamily = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(fontFamily))
        {
            config.Theme.FontFamily = fontFamily;
        }
    }

    static void ConfigureSeedData(ProjectConfig config)
    {
        Console.WriteLine("\n--- Configuring Seed Data ---");
        
        // Add default roles
        Console.Write("Add default roles (Admin, Teacher, User)? (Y/n): ");
        var addDefaultRoles = Console.ReadLine()?.Trim().ToLower();
        
        if (addDefaultRoles != "n")
        {
            config.Options.SeedRoles.Add(new SeedRole 
            { 
                Name = "Admin", 
                Description = "Administrator role with full access" 
            });
            config.Options.SeedRoles.Add(new SeedRole 
            { 
                Name = "Seller", 
                Description = "Seller role with product management access" 
            });
            config.Options.SeedRoles.Add(new SeedRole 
            { 
                Name = "User", 
                Description = "Regular user role" 
            });
        }

        // Add admin user
        Console.Write("Create default admin user? (Y/n): ");
        var addAdmin = Console.ReadLine()?.Trim().ToLower();
        
        if (addAdmin != "n")
        {
            Console.Write("Admin email (default: admin@example.com): ");
            var email = Console.ReadLine()?.Trim();
            email = string.IsNullOrEmpty(email) ? "admin@example.com" : email;

            Console.Write("Admin password (default: Admin@123): ");
            var password = Console.ReadLine()?.Trim();
            password = string.IsNullOrEmpty(password) ? "Admin@123" : password;

            config.Options.SeedUsers.Add(new SeedUser
            {
                Username = "admin",
                Email = email,
                PhoneNumber = "09123456789",
                Password = password,
                Roles = new List<string> { "Admin" }
            });
        }
    }

    static ProjectConfig? ParseCommandLineArguments(string[] args)
    {
        var config = new ProjectConfig();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-n":
                case "--name":
                    if (i + 1 < args.Length)
                        config.ProjectName = args[++i];
                    break;
                case "-o":
                case "--output":
                    if (i + 1 < args.Length)
                        config.OutputPath = args[++i];
                    break;
                case "--namespace":
                    if (i + 1 < args.Length)
                        config.Namespace = args[++i];
                    break;
                case "--no-web":
                    config.Options.IncludeWebSite = false;
                    break;
                case "--no-tests":
                    config.Options.IncludeTests = false;
                    break;
                case "--seed-data":
                    config.Options.GenerateInitialSeedData = true;
                    break;
            }
        }

        if (string.IsNullOrEmpty(config.ProjectName))
        {
            return null;
        }

        if (string.IsNullOrEmpty(config.OutputPath))
        {
            config.OutputPath = Path.Combine(Directory.GetCurrentDirectory(), config.ProjectName);
        }

        if (string.IsNullOrEmpty(config.Namespace))
        {
            config.Namespace = config.ProjectName;
        }

        return config;
    }

    static void ValidateConfiguration(ProjectConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ProjectName))
        {
            throw new ArgumentException("Project name is required");
        }

        if (string.IsNullOrWhiteSpace(config.OutputPath))
        {
            throw new ArgumentException("Output path is required");
        }

        if (Directory.Exists(config.OutputPath))
        {
            Console.WriteLine($"Warning: Directory already exists: {config.OutputPath}");
            Console.Write("Continue? (y/N): ");
            var response = Console.ReadLine()?.Trim().ToLower();
            if (response != "y")
            {
                throw new OperationCanceledException("Operation cancelled by user");
            }
        }
    }

    static void ShowUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  ProjectGenerator [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --interactive              Run in interactive mode (default)");
        Console.WriteLine("  --config <file>           Load configuration from JSON file");
        Console.WriteLine("  -n, --name <name>         Project name");
        Console.WriteLine("  -o, --output <path>       Output directory path");
        Console.WriteLine("  --namespace <namespace>   Root namespace (defaults to project name)");
        Console.WriteLine("  --no-web                  Don't include WebSite project");
        Console.WriteLine("  --no-tests                Don't include Tests project");
        Console.WriteLine("  --seed-data               Generate seed data configuration");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  ProjectGenerator");
        Console.WriteLine("  ProjectGenerator --interactive");
        Console.WriteLine("  ProjectGenerator -n MyProject -o C:\\Projects");
        Console.WriteLine("  ProjectGenerator --config project-config.json");
        Console.WriteLine("  ProjectGenerator -n MyProject --seed-data --no-tests");
    }
}
