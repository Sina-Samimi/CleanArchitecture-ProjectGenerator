using ProjectGenerator.Models;
using Newtonsoft.Json;

namespace ProjectGenerator.Generators;

public class SeedDataGenerator
{
    private readonly ProjectConfig _config;

    public SeedDataGenerator(ProjectConfig config)
    {
        _config = config;
    }

    public void Generate()
    {
        Console.WriteLine("Generating seed data configuration...");

        var infrastructurePath = Path.Combine(_config.OutputPath, "src", "Infrastructure");
        var seedDataPath = Path.Combine(infrastructurePath, "Data", "SeedData");
        
        Directory.CreateDirectory(seedDataPath);

        // Generate seed data class
        GenerateSeedDataClass(seedDataPath);

        // Generate JSON configuration
        GenerateSeedDataJson(seedDataPath);

        Console.WriteLine("✓ Seed data configuration created");
    }

    private void GenerateSeedDataClass(string seedDataPath)
    {
        var content = $@"using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace {_config.Namespace}.Infrastructure.Data.SeedData;

public class DatabaseSeeder
{{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _configuration;

    public DatabaseSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration)
    {{
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }}

    public async Task SeedAsync()
    {{
        await SeedRolesAsync();
        await SeedUsersAsync();
    }}

    private async Task SeedRolesAsync()
    {{
        var rolesJson = File.ReadAllText(""Data/SeedData/roles.json"");
        var roles = JsonConvert.DeserializeObject<List<RoleSeedData>>(rolesJson) ?? new();

        foreach (var roleData in roles)
        {{
            if (!await _roleManager.RoleExistsAsync(roleData.Name))
            {{
                var role = new ApplicationRole
                {{
                    Name = roleData.Name,
                    Description = roleData.Description
                }};

                await _roleManager.CreateAsync(role);
                Console.WriteLine($""Role created: {{roleData.Name}}"");
            }}
        }}
    }}

    private async Task SeedUsersAsync()
    {{
        var usersJson = File.ReadAllText(""Data/SeedData/users.json"");
        var users = JsonConvert.DeserializeObject<List<UserSeedData>>(usersJson) ?? new();

        foreach (var userData in users)
        {{
            var existingUser = await _userManager.FindByEmailAsync(userData.Email);
            if (existingUser == null)
            {{
                var user = new ApplicationUser
                {{
                    UserName = userData.Username,
                    Email = userData.Email,
                    PhoneNumber = userData.PhoneNumber,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true
                }};

                var result = await _userManager.CreateAsync(user, userData.Password);
                
                if (result.Succeeded)
                {{
                    // Add roles
                    if (userData.Roles.Any())
                    {{
                        await _userManager.AddToRolesAsync(user, userData.Roles);
                    }}
                    
                    Console.WriteLine($""User created: {{userData.Email}}"");
                }}
            }}
        }}
    }}
}}

public class UserSeedData
{{
    public string Username {{ get; set; }} = string.Empty;
    public string Email {{ get; set; }} = string.Empty;
    public string PhoneNumber {{ get; set; }} = string.Empty;
    public string Password {{ get; set; }} = string.Empty;
    public List<string> Roles {{ get; set; }} = new();
}}

public class RoleSeedData
{{
    public string Name {{ get; set; }} = string.Empty;
    public string Description {{ get; set; }} = string.Empty;
    public List<string> Permissions {{ get; set; }} = new();
}}
";

        File.WriteAllText(Path.Combine(seedDataPath, "DatabaseSeeder.cs"), content);
    }

    private void GenerateSeedDataJson(string seedDataPath)
    {
        // Generate roles.json
        var rolesJson = JsonConvert.SerializeObject(_config.Options.SeedRoles, Formatting.Indented);
        File.WriteAllText(Path.Combine(seedDataPath, "roles.json"), rolesJson);

        // Generate users.json
        var usersJson = JsonConvert.SerializeObject(_config.Options.SeedUsers, Formatting.Indented);
        File.WriteAllText(Path.Combine(seedDataPath, "users.json"), usersJson);
    }
}
