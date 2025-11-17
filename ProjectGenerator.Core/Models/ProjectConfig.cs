namespace ProjectGenerator.Core.Models;

public class ProjectConfig
{
    public string ProjectName { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public GenerationOptions Options { get; set; } = new();
    public ThemeSettings Theme { get; set; } = new();
}

public class GenerationOptions
{
    public bool IncludeWebSite { get; set; } = true;
    public bool IncludeTests { get; set; } = true;
    public bool GenerateInitialSeedData { get; set; } = false;
    public List<SeedUser> SeedUsers { get; set; } = new();
    public List<SeedRole> SeedRoles { get; set; } = new();
    public List<SeedCategory> SeedCategories { get; set; } = new();
    public List<SeedProduct> SeedProducts { get; set; } = new();
    public List<SeedBlogCategory> SeedBlogCategories { get; set; } = new();
    public List<SeedBlogPost> SeedBlogPosts { get; set; } = new();
    public List<SeedSeller> SeedSellers { get; set; } = new();
    public List<SeedPermission> SeedPermissions { get; set; } = new();
    public List<SeedSiteSetting> SeedSiteSettings { get; set; } = new();
    public SeedDataConfig SeedData { get; set; } = new();
    public ProjectFeatures Features { get; set; } = new();
}

public class ThemeSettings
{
    public string PrimaryColor { get; set; } = "#007bff";
    public string SecondaryColor { get; set; } = "#6c757d";
    public string SuccessColor { get; set; } = "#28a745";
    public string DangerColor { get; set; } = "#dc3545";
    public string WarningColor { get; set; } = "#ffc107";
    public string InfoColor { get; set; } = "#17a2b8";
    public string LightColor { get; set; } = "#f8f9fa";
    public string DarkColor { get; set; } = "#343a40";
    public string BackgroundColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#212529";
    public string FontFamily { get; set; } = "Vazirmatn, Tahoma, Arial, sans-serif";
    public string SiteName { get; set; } = "فروشگاه من";
    public string SiteDescription { get; set; } = "فروشگاه آنلاین";
    public string LogoUrl { get; set; } = "/images/logo.png";
    public string FaviconUrl { get; set; } = "/images/favicon.ico";
}

public class SeedDataConfig
{
    public bool SeedCategories { get; set; } = true;
    public bool SeedProducts { get; set; } = true;
    public bool SeedBlogCategories { get; set; } = true;
    public bool SeedBlogPosts { get; set; } = true;
    public bool SeedSellers { get; set; } = true;
    public bool SeedPermissions { get; set; } = true;
    public bool SeedSiteSettings { get; set; } = true;
    public bool SeedNavigationMenu { get; set; } = true;
    public int ProductCount { get; set; } = 10;
    public int BlogPostCount { get; set; } = 5;
    public int CategoryCount { get; set; } = 5;
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

public class SeedCategory
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
}

public class SeedProduct
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class SeedBlogCategory
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
}

public class SeedBlogPost
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class SeedSeller
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public string? Specialty { get; set; }
    public string? Bio { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? UserId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SeedPermission
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Group { get; set; } = "General";
    public bool IsCore { get; set; } = false;
}

public class SeedSiteSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
