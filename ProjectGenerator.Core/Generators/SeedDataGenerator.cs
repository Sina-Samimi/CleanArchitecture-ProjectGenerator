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
        Console.WriteLine("Generating comprehensive seed data configuration...");

        var infrastructurePath = Path.Combine(_config.OutputPath, "src", "Infrastructure");
        var seedDataPath = Path.Combine(infrastructurePath, "Data", "SeedData");
        var migrationsPath = Path.Combine(infrastructurePath, "Data", "Migrations");
        
        Directory.CreateDirectory(seedDataPath);
        Directory.CreateDirectory(migrationsPath);

        // Generate seed data class with all entities
        GenerateSeedDataClass(seedDataPath);

        // Generate initial migration
        GenerateInitialMigration(migrationsPath);

        // Generate JSON configuration files
        GenerateSeedDataJson(seedDataPath);

        // Generate default seed data if not provided
        GenerateDefaultSeedData();

        Console.WriteLine("✓ Comprehensive seed data configuration created");
    }

    private void GenerateSeedDataClass(string seedDataPath)
    {
        var features = _config.Options.Features;
        var seedConfig = _config.Options.SeedData;

        var content = $@"using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using {_config.Namespace}.Domain.Entities;
using {_config.Namespace}.Domain.Enums;
using {_config.Namespace}.Infrastructure.Data;
using ApplicationUser = {_config.Namespace}.Infrastructure.Data.ApplicationUser;
using ApplicationRole = {_config.Namespace}.Infrastructure.Data.ApplicationRole;

namespace {_config.Namespace}.Infrastructure.Data.SeedData;

public class DatabaseSeeder
{{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _configuration;

    public DatabaseSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration)
    {{
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }}

    public async Task SeedAsync()
    {{
        // Ensure database is created
        await _context.Database.EnsureCreatedAsync();

        // Seed in order
        await SeedRolesAsync();
        await SeedUsersAsync();
        
        {(features.ProductCatalog ? @"await SeedCategoriesAsync();
        await SeedProductsAsync();" : "")}
        
        {(features.SellerPanel ? @"await SeedSellersAsync();" : "")}
        
        {(features.BlogSystem ? @"await SeedBlogCategoriesAsync();
        await SeedBlogPostsAsync();" : "")}

        await _context.SaveChangesAsync();
        Console.WriteLine(""✓ Database seeded successfully"");
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
                
                if (result.Succeeded && userData.Roles.Any())
                {{
                    await _userManager.AddToRolesAsync(user, userData.Roles);
                    Console.WriteLine($""User created: {{userData.Email}}"");
                }}
            }}
        }}
    }}

    private async Task SeedPermissionsAsync()
    {{
        // Permissions are managed through ASP.NET Core Identity Role Claims
        // This method is kept for future extension if needed
        await Task.CompletedTask;
    }}

    {(features.ProductCatalog ? $@"
    private async Task SeedCategoriesAsync()
    {{
        var seedConfig = _configuration.GetSection(""SeedData:SeedCategories"");
        if (!seedConfig.Exists() || !bool.Parse(seedConfig.Value ?? ""true"")) return;

        var categoriesJson = File.ReadAllText(""Data/SeedData/categories.json"");
        var categories = JsonConvert.DeserializeObject<List<CategorySeedData>>(categoriesJson) ?? new();

        foreach (var catData in categories)
        {{
            var existing = await _context.Set<Category>()
                .FirstOrDefaultAsync(c => c.Slug == catData.Slug);
            
            if (existing == null)
            {{
                var category = new Category
                {{
                    Name = catData.Name,
                    Slug = catData.Slug,
                    Description = catData.Description,
                    ParentCategoryId = catData.ParentId,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                }};

                await _context.Set<Category>().AddAsync(category);
                await _context.SaveChangesAsync(); // Save to get the ID
                
                // Update parent reference if needed
                if (catData.ParentId.HasValue)
                {{
                    var parent = await _context.Set<Category>()
                        .FirstOrDefaultAsync(c => c.Id == catData.ParentId.Value);
                    if (parent != null)
                    {{
                        category.ParentCategoryId = parent.Id;
                    }}
                }}
                
                Console.WriteLine($""Category created: {{catData.Name}}"");
            }}
        }}
    }}

    private async Task SeedProductsAsync()
    {{
        var seedConfig = _configuration.GetSection(""SeedData:SeedProducts"");
        if (!seedConfig.Exists() || !bool.Parse(seedConfig.Value ?? ""true"")) return;

        var productsJson = File.ReadAllText(""Data/SeedData/products.json"");
        var products = JsonConvert.DeserializeObject<List<ProductSeedData>>(productsJson) ?? new();

        // Get first admin user as seller
        var adminUser = await _userManager.FindByNameAsync(""admin"");
        var sellerId = adminUser?.Id ?? 1;

        foreach (var prodData in products)
        {{
            var existing = await _context.Set<Product>()
                .FirstOrDefaultAsync(p => p.Slug == prodData.Slug);
            
            if (existing == null)
            {{
                var product = new Product
                {{
                    Title = prodData.Name,
                    Slug = prodData.Slug,
                    Description = prodData.Description ?? string.Empty,
                    ShortDescription = prodData.Description?.Length > 100 ? prodData.Description.Substring(0, 100) : prodData.Description,
                    Price = prodData.Price,
                    Stock = 10,
                    CategoryId = prodData.CategoryId,
                    SellerId = sellerId,
                    IsActive = prodData.IsPublished,
                    CreatedDate = DateTime.UtcNow
                }};

                await _context.Set<Product>().AddAsync(product);
                Console.WriteLine($""Product created: {{prodData.Name}}"");
            }}
        }}
    }}" : "")}

    {(features.SellerPanel ? $@"
    private async Task SeedSellersAsync()
    {{
        // Sellers are ApplicationUsers with Seller role
        // This method ensures users with Seller role exist
        var seedConfig = _configuration.GetSection(""SeedData:SeedSellers"");
        if (!seedConfig.Exists() || !bool.Parse(seedConfig.Value ?? ""true"")) return;

        var sellersJson = File.ReadAllText(""Data/SeedData/sellers.json"");
        var sellers = JsonConvert.DeserializeObject<List<SellerSeedData>>(sellersJson) ?? new();

        foreach (var sellerData in sellers)
        {{
            var existingUser = await _userManager.FindByEmailAsync(sellerData.ContactEmail ?? """");
            if (existingUser == null && !string.IsNullOrEmpty(sellerData.ContactEmail))
            {{
                var user = new ApplicationUser
                {{
                    UserName = sellerData.ContactEmail,
                    Email = sellerData.ContactEmail,
                    PhoneNumber = sellerData.ContactPhone,
                    EmailConfirmed = true
                }};

                var result = await _userManager.CreateAsync(user, ""Seller@123"");
                if (result.Succeeded)
                {{
                    if (!await _roleManager.RoleExistsAsync(""Seller""))
                    {{
                        await _roleManager.CreateAsync(new ApplicationRole {{ Name = ""Seller"" }});
                    }}
                    await _userManager.AddToRoleAsync(user, ""Seller"");
                    Console.WriteLine($""Seller user created: {{sellerData.ContactEmail}}"");
                }}
            }}
        }}
    }}" : "")}

    {(features.BlogSystem ? $@"
    private async Task SeedBlogCategoriesAsync()
    {{
        var seedConfig = _configuration.GetSection(""SeedData:SeedBlogCategories"");
        if (!seedConfig.Exists() || !bool.Parse(seedConfig.Value ?? ""true"")) return;

        var categoriesJson = File.ReadAllText(""Data/SeedData/blog-categories.json"");
        var categories = JsonConvert.DeserializeObject<List<BlogCategorySeedData>>(categoriesJson) ?? new();

        foreach (var catData in categories)
        {{
            var existing = await _context.Set<BlogCategory>()
                .FirstOrDefaultAsync(c => c.Slug == catData.Slug);
            
            if (existing == null)
            {{
                var category = new BlogCategory
                {{
                    Name = catData.Name,
                    Slug = catData.Slug,
                    Description = catData.Description,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                }};

                await _context.Set<BlogCategory>().AddAsync(category);
                Console.WriteLine($""Blog Category created: {{catData.Name}}"");
            }}
        }}
    }}

    private async Task SeedBlogPostsAsync()
    {{
        var seedConfig = _configuration.GetSection(""SeedData:SeedBlogPosts"");
        if (!seedConfig.Exists() || !bool.Parse(seedConfig.Value ?? ""true"")) return;

        var postsJson = File.ReadAllText(""Data/SeedData/blog-posts.json"");
        var posts = JsonConvert.DeserializeObject<List<BlogPostSeedData>>(postsJson) ?? new();

        // Get first admin user as author
        var adminUser = await _userManager.FindByNameAsync(""admin"");
        var authorId = adminUser?.Id ?? 1;

        foreach (var postData in posts)
        {{
            var existing = await _context.Set<Blog>()
                .FirstOrDefaultAsync(p => p.Slug == postData.Slug);
            
            if (existing == null)
            {{
                var post = new Blog
                {{
                    Title = postData.Title,
                    Slug = postData.Slug,
                    Summary = postData.Summary,
                    Content = postData.Content,
                    AuthorId = authorId,
                    Status = postData.IsPublished ? BlogStatus.Published : BlogStatus.Draft,
                    PublishedDate = postData.IsPublished ? DateTime.UtcNow : null,
                    CreatedDate = DateTime.UtcNow
                }};

                await _context.Set<Blog>().AddAsync(post);
                Console.WriteLine($""Blog Post created: {{postData.Title}}"");
            }}
        }}
    }}" : "")}

    private async Task SeedSiteSettingsAsync()
    {{
        // Site settings can be stored in appsettings.json or a separate settings table
        // This method is kept for future extension
        await Task.CompletedTask;
    }}

    private async Task SeedNavigationMenuAsync()
    {{
        // Navigation menu can be configured in appsettings.json or a separate menu table
        // This method is kept for future extension
        await Task.CompletedTask;
    }}
}}

// Seed Data Models
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


{(features.ProductCatalog ? @"
public class CategorySeedData
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
}

public class ProductSeedData
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public bool IsPublished { get; set; } = true;
}" : "")}

{(features.SellerPanel ? @"
public class SellerSeedData
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Degree { get; set; }
    public string? Specialty { get; set; }
    public string? Bio { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? UserId { get; set; }
    public bool IsActive { get; set; } = true;
}" : "")}

{(features.BlogSystem ? @"
public class BlogCategorySeedData
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class BlogPostSeedData
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
}" : "")}

";

        File.WriteAllText(Path.Combine(seedDataPath, "DatabaseSeeder.cs"), content);
    }

    private void GenerateInitialMigration(string migrationsPath)
    {
        var content = $@"using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace {_config.Namespace}.Infrastructure.Data.Migrations;

public partial class InitialCreate : Migration
{{
    protected override void Up(MigrationBuilder migrationBuilder)
    {{
        // This migration will be generated by EF Core
        // Run: dotnet ef migrations add InitialCreate
        // Then: dotnet ef database update
    }}

    protected override void Down(MigrationBuilder migrationBuilder)
    {{
        // Drop all tables
    }}
}}
";

        File.WriteAllText(Path.Combine(migrationsPath, "InitialCreate.cs"), content);
    }

    private void GenerateSeedDataJson(string seedDataPath)
    {
        // Generate roles.json
        var rolesJson = JsonConvert.SerializeObject(_config.Options.SeedRoles, Formatting.Indented);
        File.WriteAllText(Path.Combine(seedDataPath, "roles.json"), rolesJson);

        // Generate users.json
        var usersJson = JsonConvert.SerializeObject(_config.Options.SeedUsers, Formatting.Indented);
        File.WriteAllText(Path.Combine(seedDataPath, "users.json"), usersJson);

        // Generate permissions.json
        var permissionsJson = JsonConvert.SerializeObject(_config.Options.SeedPermissions, Formatting.Indented);
        File.WriteAllText(Path.Combine(seedDataPath, "permissions.json"), permissionsJson);

        if (_config.Options.Features.ProductCatalog)
        {
            // Generate categories.json
            var categoriesJson = JsonConvert.SerializeObject(_config.Options.SeedCategories, Formatting.Indented);
            File.WriteAllText(Path.Combine(seedDataPath, "categories.json"), categoriesJson);

            // Generate products.json
            var productsJson = JsonConvert.SerializeObject(_config.Options.SeedProducts, Formatting.Indented);
            File.WriteAllText(Path.Combine(seedDataPath, "products.json"), productsJson);
        }

        if (_config.Options.Features.SellerPanel)
        {
            // Generate sellers.json
            var sellersJson = JsonConvert.SerializeObject(_config.Options.SeedSellers, Formatting.Indented);
            File.WriteAllText(Path.Combine(seedDataPath, "sellers.json"), sellersJson);
        }

        if (_config.Options.Features.BlogSystem)
        {
            // Generate blog-categories.json
            var blogCategoriesJson = JsonConvert.SerializeObject(_config.Options.SeedBlogCategories, Formatting.Indented);
            File.WriteAllText(Path.Combine(seedDataPath, "blog-categories.json"), blogCategoriesJson);

            // Generate blog-posts.json
            var blogPostsJson = JsonConvert.SerializeObject(_config.Options.SeedBlogPosts, Formatting.Indented);
            File.WriteAllText(Path.Combine(seedDataPath, "blog-posts.json"), blogPostsJson);
        }

        // Generate site-settings.json
        var siteSettingsJson = JsonConvert.SerializeObject(_config.Options.SeedSiteSettings, Formatting.Indented);
        File.WriteAllText(Path.Combine(seedDataPath, "site-settings.json"), siteSettingsJson);

        // Generate navigation-menu.json
        var navigationMenuJson = JsonConvert.SerializeObject(new List<object>(), Formatting.Indented);
        File.WriteAllText(Path.Combine(seedDataPath, "navigation-menu.json"), navigationMenuJson);
    }

    private void GenerateDefaultSeedData()
    {
        // Generate default seed data if lists are empty
        if (!_config.Options.SeedRoles.Any())
        {
            _config.Options.SeedRoles.AddRange(new[]
            {
                new SeedRole { Name = "Admin", Description = "Administrator role with full access", Permissions = new List<string>() },
                new SeedRole { Name = "Seller", Description = "Seller role with product management access", Permissions = new List<string>() },
                new SeedRole { Name = "User", Description = "Regular user role", Permissions = new List<string>() }
            });
        }

        if (!_config.Options.SeedUsers.Any())
        {
            _config.Options.SeedUsers.Add(new SeedUser
            {
                Username = "admin",
                Email = "admin@example.com",
                PhoneNumber = "09123456789",
                Password = "Admin@123",
                Roles = new List<string> { "Admin" }
            });
        }

        if (_config.Options.Features.ProductCatalog && !_config.Options.SeedCategories.Any() && _config.Options.SeedData.SeedCategories)
        {
            _config.Options.SeedCategories.AddRange(new[]
            {
                new SeedCategory { Name = "الکترونیک", Slug = "electronics", Description = "دسته‌بندی محصولات الکترونیک" },
                new SeedCategory { Name = "پوشاک", Slug = "clothing", Description = "دسته‌بندی پوشاک" },
                new SeedCategory { Name = "کتاب", Slug = "books", Description = "دسته‌بندی کتاب‌ها" }
            });
        }

        if (_config.Options.Features.BlogSystem && !_config.Options.SeedBlogCategories.Any() && _config.Options.SeedData.SeedBlogCategories)
        {
            _config.Options.SeedBlogCategories.AddRange(new[]
            {
                new SeedBlogCategory { Name = "عمومی", Slug = "general", Description = "دسته‌بندی عمومی" },
                new SeedBlogCategory { Name = "فناوری", Slug = "technology", Description = "دسته‌بندی فناوری" }
            });
        }

        if (!_config.Options.SeedSiteSettings.Any() && _config.Options.SeedData.SeedSiteSettings)
        {
            _config.Options.SeedSiteSettings.AddRange(new[]
            {
                new SeedSiteSetting { Key = "SiteName", Value = _config.Theme.SiteName, Description = "نام سایت" },
                new SeedSiteSetting { Key = "SiteDescription", Value = _config.Theme.SiteDescription, Description = "توضیحات سایت" },
                new SeedSiteSetting { Key = "ContactEmail", Value = "info@example.com", Description = "ایمیل تماس" },
                new SeedSiteSetting { Key = "ContactPhone", Value = "021-12345678", Description = "تلفن تماس" }
            });
        }
    }
}