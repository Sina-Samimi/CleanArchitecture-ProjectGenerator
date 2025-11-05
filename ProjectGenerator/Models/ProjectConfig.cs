namespace ProjectGenerator.Models;

public class ProjectConfig
{
    public string ProjectName { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public GenerationOptions Options { get; set; } = new();
}

public class GenerationOptions
{
    public bool IncludeWebSite { get; set; } = true;
    public bool IncludeTests { get; set; } = true;
    public bool GenerateInitialSeedData { get; set; } = false;
    public List<SeedUser> SeedUsers { get; set; } = new();
    public List<SeedRole> SeedRoles { get; set; } = new();
    public ProjectFeatures Features { get; set; } = new();
}

public class ProjectFeatures
{
    public bool UserManagement { get; set; } = true;
    public bool SellerPanel { get; set; } = true;
    public bool ProductCatalog { get; set; } = true;
    public bool ShoppingCart { get; set; } = true;
    public bool Invoicing { get; set; } = true;
    public bool BlogSystem { get; set; } = true;
}

public class SeedUser
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = "Admin@123"; // Default password
    public List<string> Roles { get; set; } = new();
}

public class SeedRole
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}
