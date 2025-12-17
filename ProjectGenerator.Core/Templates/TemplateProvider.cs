namespace ProjectGenerator.Core.Templates;

public partial class TemplateProvider
{
    private readonly string _namespace;

    public TemplateProvider(string namespaceName)
    {
        _namespace = namespaceName;
    }

    public string GetBasicCsprojTemplate(string projectName)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.Extensions.Identity.Stores"" Version=""9.0.0"" />
  </ItemGroup>

</Project>";
    }

    public string GetEntityBaseClassTemplate()
    {
        return $@"using System;
using System.Net;

namespace {_namespace}.Domain.Base;

public abstract class Entity
{{
    public Guid Id {{ get; protected set; }} = Guid.NewGuid();
    public DateTimeOffset CreateDate {{ get; protected set; }} = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdateDate {{ get; protected set; }} = DateTimeOffset.UtcNow;
    public IPAddress Ip {{ get; protected set; }} = IPAddress.None;
}}
";
    }

    public string GetApplicationCsprojTemplate(string projectName)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""..\Domain\Domain.csproj"" />
    <ProjectReference Include=""..\SharedKernel\SharedKernel.csproj"" />
  </ItemGroup>

    <ItemGroup>
      <PackageReference Include=""FluentValidation"" Version=""11.9.0"" />
      <PackageReference Include=""MediatR"" Version=""12.2.0"" />
      <PackageReference Include=""AutoMapper"" Version=""12.0.1"" />
      <PackageReference Include=""AutoMapper.Extensions.Microsoft.DependencyInjection"" Version=""12.0.1"" />
    </ItemGroup>

</Project>";
    }

    public string GetInfrastructureCsprojTemplate(string projectName)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""..\Application\Application.csproj"" />
    <ProjectReference Include=""..\Domain\Domain.csproj"" />
    <ProjectReference Include=""..\SharedKernel\SharedKernel.csproj"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Identity.EntityFrameworkCore"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Tools"" Version=""9.0.0"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
  </ItemGroup>

</Project>";
    }

    public string GetTestsCsprojTemplate(string projectName)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.9.0"" />
    <PackageReference Include=""xunit"" Version=""2.6.6"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.5.6"">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include=""Moq"" Version=""4.20.70"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\..\src\Domain\Domain.csproj"" />
    <ProjectReference Include=""..\..\src\Application\Application.csproj"" />
    <ProjectReference Include=""..\..\src\Infrastructure\Infrastructure.csproj"" />
  </ItemGroup>

</Project>";
    }

    public string GetWebSiteCsprojTemplate(string projectName)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""..\Application\Application.csproj"" />
    <ProjectReference Include=""..\Infrastructure\Infrastructure.csproj"" />
    <ProjectReference Include=""..\Domain\Domain.csproj"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Identity.EntityFrameworkCore"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""9.0.0"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>";
    }

    public string GetBaseEntityTemplate()
    {
        return $@"namespace {_namespace}.Domain.Entities;

public abstract class BaseEntity
{{
    public int Id {{ get; set; }}
    public DateTime CreatedDate {{ get; set; }} = DateTime.UtcNow;
    public DateTime? ModifiedDate {{ get; set; }}
    public bool IsDeleted {{ get; set; }} = false;
}}
";
    }

    public string GetIAggregateRootTemplate()
    {
        return $@"namespace {_namespace}.Domain.Entities;

/// <summary>
/// Marker interface to identify aggregate roots
/// </summary>
public interface IAggregateRoot
{{
}}
";
    }

    public string GetIRepositoryTemplate()
    {
        return $@"using System.Linq.Expressions;

namespace {_namespace}.SharedKernel.Interfaces;

public interface IRepository<T> where T : class
{{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}}
";
    }

    public string GetSharedKernelResultTemplate()
    {
        return $@"namespace {_namespace}.SharedKernel.BaseTypes;

public readonly record struct Result(bool IsSuccess, string? Error)
{{
    public static Result Success() => new(true, null);

    public static Result Failure(string error)
    {{
        if (string.IsNullOrWhiteSpace(error))
        {{
            throw new ArgumentException(""Error message cannot be empty"", nameof(error));
        }}

        return new Result(false, error);
    }}
}}

public readonly record struct Result<T>(bool IsSuccess, T? Value, string? Error)
{{
    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(string error)
    {{
        if (string.IsNullOrWhiteSpace(error))
        {{
            throw new ArgumentException(""Error message cannot be empty"", nameof(error));
        }}

        return new Result<T>(false, default, error);
    }}
}}
";
    }

    public string GetDbContextTemplate()
    {
        return $@"using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace {_namespace}.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {{
    }}

    // Add your DbSets here
    // public DbSet<YourEntity> YourEntities {{ get; set; }}

    protected override void OnModelCreating(ModelBuilder builder)
    {{
        base.OnModelCreating(builder);

        // Configure your entities here
        // builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }}
}}

// Placeholder classes - replace with your actual Identity classes
public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser<int>
{{
    public string? FirstName {{ get; set; }}
    public string? LastName {{ get; set; }}
}}

public class ApplicationRole : Microsoft.AspNetCore.Identity.IdentityRole<int>
{{
    public string? Description {{ get; set; }}
}}
";
    }

    public string GetGenericRepositoryTemplate()
    {
        return $@"using {_namespace}.SharedKernel.Interfaces;
using {_namespace}.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace {_namespace}.Infrastructure.Repositories;

public class GenericRepository<T> : IRepository<T> where T : class
{{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(ApplicationDbContext context)
    {{
        _context = context;
        _dbSet = context.Set<T>();
    }}

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {{
        return await _dbSet.FindAsync(new object[] {{ id }}, cancellationToken);
    }}

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {{
        return await _dbSet.ToListAsync(cancellationToken);
    }}

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {{
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }}

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {{
        await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }}

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {{
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }}

    public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {{
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }}

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {{
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }}
}}
";
    }

    public string GetProgramTemplate()
    {
        return $@"var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{{
    app.UseExceptionHandler(""/Home/Error"");
    app.UseHsts();
}}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: ""default"",
    pattern: ""{{controller=Home}}/{{action=Index}}/{{id?}}"");

app.Run();
";
    }

    public string GetEnhancedProgramTemplate()
    {
        return $@"using {_namespace}.Domain.Entities;
using {_namespace}.Application;
using {_namespace}.Infrastructure;
using {_namespace}.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(""DefaultConnection"")));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = false;
}})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Authentication
builder.Services.ConfigureApplicationCookie(options =>
{{
    options.LoginPath = ""/Account/Login"";
    options.LogoutPath = ""/Account/Logout"";
    options.AccessDeniedPath = ""/Account/AccessDenied"";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
}});

// Application & Infrastructure services (including IProductService, etc.)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add MVC/Razor & common services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
}});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{{
    app.UseExceptionHandler(""/Home/Error"");
    app.UseHsts();
}}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Seed Database
using (var scope = app.Services.CreateScope())
{{
    var services = scope.ServiceProvider;
    try
    {{
        var seeder = new {_namespace}.Infrastructure.Persistence.SeedData.DatabaseSeeder(
            services.GetRequiredService<ApplicationDbContext>(),
            services.GetRequiredService<UserManager<ApplicationUser>>(),
            services.GetRequiredService<RoleManager<IdentityRole>>(),
            services.GetRequiredService<IConfiguration>()
        );
        await seeder.SeedAsync();
    }}
    catch (Exception ex)
    {{
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, ""An error occurred while seeding the database."");
    }}
}}

// Area routes
app.MapControllerRoute(
    name: ""areas"",
    pattern: ""{{area:exists}}/{{controller=Home}}/{{action=Index}}/{{id?}}"");

// Default route
app.MapControllerRoute(
    name: ""default"",
    pattern: ""{{controller=Home}}/{{action=Index}}/{{id?}}"");

app.Run();
";
    }

    public string GetAppSettingsTemplate()
    {
        return $@"{{
  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""Server=.;Database={_namespace}Db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true""
  }},
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }},
  ""AllowedHosts"": ""*""
}}
";
    }
}
