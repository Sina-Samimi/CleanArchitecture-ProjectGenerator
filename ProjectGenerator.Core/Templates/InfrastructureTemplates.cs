namespace ProjectGenerator.Core.Templates;

public partial class TemplateProvider
{
    // ==================== Infrastructure Layer Templates ====================
    
    public string GetInfrastructureCsprojTemplate()
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>{_namespace}.Infrastructure</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""8.0.10"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""8.0.10"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Tools"" Version=""8.0.10"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include=""Microsoft.AspNetCore.Identity.EntityFrameworkCore"" Version=""8.0.10"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.Abstractions"" Version=""8.0.0"" />
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\Domain\Domain.csproj"" />
    <ProjectReference Include=""..\Application\Application.csproj"" />
  </ItemGroup>

  <ItemGroup>
    <FrameworkReference Include=""Microsoft.AspNetCore.App"" />
  </ItemGroup>

</Project>";
    }

    public string GetApplicationDbContextTemplate()
    {
        return $@"using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {{
    }}

    // Product & Catalog
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductComment> ProductComments => Set<ProductComment>();
    public DbSet<ProductExecutionStep> ProductExecutionSteps => Set<ProductExecutionStep>();
    public DbSet<ProductFaq> ProductFaqs => Set<ProductFaq>();
    public DbSet<SiteCategory> Categories => Set<SiteCategory>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();

    // Blog
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogAuthor> BlogAuthors => Set<BlogAuthor>();
    public DbSet<BlogComment> BlogComments => Set<BlogComment>();

    // Billing & Wallet
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<WalletAccount> WalletAccounts => Set<WalletAccount>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    // Settings
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<NavigationMenuItem> NavigationMenuItems => Set<NavigationMenuItem>();
    public DbSet<FinancialSettings> FinancialSettings => Set<FinancialSettings>();

    // Sellers
    public DbSet<SellerProfile> SellerProfiles => Set<SellerProfile>();

    // Permissions
    public DbSet<AccessPermission> AccessPermissions => Set<AccessPermission>();
    public DbSet<PageAccessPolicy> PageAccessPolicies => Set<PageAccessPolicy>();

    // Sessions
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder builder)
    {{
        base.OnModelCreating(builder);

        // Apply all configurations
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }}
}}";
    }

    public string GetProductEntityConfigurationTemplate()
    {
        return $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{{
    public void Configure(EntityTypeBuilder<Product> builder)
    {{
        builder.ToTable(""Products"");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Summary)
            .HasMaxLength(500);

        builder.Property(p => p.Description)
            .IsRequired();

        builder.Property(p => p.Price)
            .HasPrecision(18, 2);

        builder.Property(p => p.CompareAtPrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.SeoTitle)
            .HasMaxLength(200);

        builder.Property(p => p.SeoDescription)
            .HasMaxLength(500);

        builder.Property(p => p.SeoSlug)
            .HasMaxLength(200);

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Gallery)
            .WithOne()
            .HasForeignKey(""ProductId"")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Comments)
            .WithOne()
            .HasForeignKey(""ProductId"")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ExecutionSteps)
            .WithOne()
            .HasForeignKey(""ProductId"")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Faqs)
            .WithOne()
            .HasForeignKey(""ProductId"")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.SeoSlug);
        builder.HasIndex(p => p.IsPublished);
    }}
}}";
    }

    public string GetBlogEntityConfigurationTemplate()
    {
        return $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Infrastructure.Persistence.Configurations;

public class BlogConfiguration : IEntityTypeConfiguration<Blog>
{{
    public void Configure(EntityTypeBuilder<Blog> builder)
    {{
        builder.ToTable(""Blogs"");

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(b => b.Summary)
            .HasMaxLength(500);

        builder.Property(b => b.Content)
            .IsRequired();

        builder.Property(b => b.SeoTitle)
            .HasMaxLength(200);

        builder.Property(b => b.SeoDescription)
            .HasMaxLength(500);

        builder.Property(b => b.SeoSlug)
            .HasMaxLength(200);

        builder.HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Author)
            .WithMany()
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Comments)
            .WithOne()
            .HasForeignKey(""BlogId"")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.SeoSlug);
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.PublishedAt);
    }}
}}";
    }

    public string GetInvoiceEntityConfigurationTemplate()
    {
        return $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {{
        builder.ToTable(""Invoices"");

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.AdjustmentAmount)
            .HasPrecision(18, 2);

        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Items)
            .WithOne()
            .HasForeignKey(""InvoiceId"")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Transactions)
            .WithOne()
            .HasForeignKey(""InvoiceId"")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.IssueDate);
    }}
}}";
    }

    public string GetSiteCategoryEntityConfigurationTemplate()
    {
        return $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Infrastructure.Persistence.Configurations;

public class SiteCategoryConfiguration : IEntityTypeConfiguration<SiteCategory>
{{
    public void Configure(EntityTypeBuilder<SiteCategory> builder)
    {{
        builder.ToTable(""SiteCategories"");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Slug)
            .HasMaxLength(200);

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.Slug);
        builder.HasIndex(c => c.Scope);
    }}
}}";
    }

    public string GetAccessPermissionEntityConfigurationTemplate()
    {
        return $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Infrastructure.Persistence.Configurations;

public class AccessPermissionConfiguration : IEntityTypeConfiguration<AccessPermission>
{{
    public void Configure(EntityTypeBuilder<AccessPermission> builder)
    {{
        builder.ToTable(""AccessPermissions"");

        builder.Property(p => p.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.GroupKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.GroupDisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(p => p.Key)
            .IsUnique();

        builder.HasIndex(p => p.GroupKey);
    }}
}}";
    }

    public string GetPageAccessPolicyEntityConfigurationTemplate()
    {
        return $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Infrastructure.Persistence.Configurations;

public class PageAccessPolicyConfiguration : IEntityTypeConfiguration<PageAccessPolicy>
{{
    public void Configure(EntityTypeBuilder<PageAccessPolicy> builder)
    {{
        builder.ToTable(""PageAccessPolicies"");

        builder.Property(p => p.Area)
            .HasMaxLength(100);

        builder.Property(p => p.Controller)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.PermissionKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => new {{ p.Area, p.Controller, p.Action }})
            .IsUnique();
    }}
}}";
    }

    public string GetWalletAccountEntityConfigurationTemplate()
    {
        return $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Infrastructure.Persistence.Configurations;

public class WalletAccountConfiguration : IEntityTypeConfiguration<WalletAccount>
{{
    public void Configure(EntityTypeBuilder<WalletAccount> builder)
    {{
        builder.ToTable(""WalletAccounts"");

        builder.Property(w => w.Balance)
            .HasPrecision(18, 2);

        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Transactions)
            .WithOne()
            .HasForeignKey(""WalletAccountId"")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.UserId)
            .IsUnique();
    }}
}}";
    }

    public string GetSellerProfileEntityConfigurationTemplate()
    {
        return $@"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Infrastructure.Persistence.Configurations;

public class SellerProfileConfiguration : IEntityTypeConfiguration<SellerProfile>
{{
    public void Configure(EntityTypeBuilder<SellerProfile> builder)
    {{
        builder.ToTable(""SellerProfiles"");

        builder.Property(s => s.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Degree)
            .HasMaxLength(200);

        builder.Property(s => s.Specialty)
            .HasMaxLength(200);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.IsActive);
    }}
}}";
    }

    // ==================== Infrastructure Services ====================
    
    public string GetFileServiceTemplate()
    {
        return $@"using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using {_namespace}.Application.Services;

namespace {_namespace}.Infrastructure.Services;

public class FileService : IFileService
{{
    private readonly IWebHostEnvironment _environment;
    private readonly string _uploadsPath;

    public FileService(IWebHostEnvironment environment)
    {{
        _environment = environment;
        _uploadsPath = Path.Combine(_environment.WebRootPath, ""uploads"");
        
        if (!Directory.Exists(_uploadsPath))
        {{
            Directory.CreateDirectory(_uploadsPath);
        }}
    }}

    public async Task<string?> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {{
        if (file == null || file.Length == 0)
        {{
            return null;
        }}

        var folderPath = Path.Combine(_uploadsPath, folder);
        if (!Directory.Exists(folderPath))
        {{
            Directory.CreateDirectory(folderPath);
        }}

        var fileName = $""{{Guid.NewGuid()}}{{Path.GetExtension(file.FileName)}}"";
        var filePath = Path.Combine(folderPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {{
            await file.CopyToAsync(stream, cancellationToken);
        }}

        return $""/uploads/{{folder}}/{{fileName}}"";
    }}

    public Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {{
        try
        {{
            var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
            if (File.Exists(fullPath))
            {{
                File.Delete(fullPath);
                return Task.FromResult(true);
            }}
            return Task.FromResult(false);
        }}
        catch
        {{
            return Task.FromResult(false);
        }}
    }}

    public string GetFileUrl(string filePath)
    {{
        return filePath;
    }}
}}";
    }

    public string GetOtpServiceTemplate()
    {
        return $@"using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using {_namespace}.Application.Services;

namespace {_namespace}.Infrastructure.Services;

public class OtpService : IOtpService
{{
    private readonly IDistributedCache _cache;
    private const int OtpExpirationMinutes = 2;

    public OtpService(IDistributedCache cache)
    {{
        _cache = cache;
    }}

    public string GenerateOtp()
    {{
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }}

    public async Task<bool> StoreOtpAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default)
    {{
        var key = GetCacheKey(phoneNumber);
        var options = new DistributedCacheEntryOptions
        {{
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OtpExpirationMinutes)
        }};

        await _cache.SetStringAsync(key, otp, options, cancellationToken);
        return true;
    }}

    public async Task<bool> ValidateOtpAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default)
    {{
        var key = GetCacheKey(phoneNumber);
        var storedOtp = await _cache.GetStringAsync(key, cancellationToken);

        if (storedOtp == null || storedOtp != otp)
        {{
            return false;
        }}

        await _cache.RemoveAsync(key, cancellationToken);
        return true;
    }}

    private static string GetCacheKey(string phoneNumber)
    {{
        return $""otp:{{phoneNumber}}"";
    }}
}}";
    }

    public string GetSmsServiceTemplate()
    {
        return $@"using Microsoft.Extensions.Logging;
using {_namespace}.Application.Services;

namespace {_namespace}.Infrastructure.Services;

public class SmsService : ISmsService
{{
    private readonly ILogger<SmsService> _logger;

    public SmsService(ILogger<SmsService> logger)
    {{
        _logger = logger;
    }}

    public Task<bool> SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {{
        // TODO: Implement actual SMS sending logic here (e.g., Kavenegar, Ghasedak, etc.)
        _logger.LogInformation(""Sending OTP {{Code}} to {{PhoneNumber}}"", code, phoneNumber);
        
        // For development, just return success
        return Task.FromResult(true);
    }}

    public Task<bool> SendMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {{
        // TODO: Implement actual SMS sending logic here
        _logger.LogInformation(""Sending message to {{PhoneNumber}}: {{Message}}"", phoneNumber, message);
        
        // For development, just return success
        return Task.FromResult(true);
    }}
}}";
    }

    public string GetInfrastructureExtensionsTemplate()
    {
        return $@"using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using {_namespace}.Application.Services;
using {_namespace}.Infrastructure.Persistence;
using {_namespace}.Infrastructure.Services;

namespace {_namespace}.Infrastructure;

public static class ServiceCollectionExtensions
{{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {{
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString(""DefaultConnection""),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Services
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ISmsService, SmsService>();

        // Cache
        services.AddDistributedMemoryCache();

        return services;
    }}
}}";
    }
}
