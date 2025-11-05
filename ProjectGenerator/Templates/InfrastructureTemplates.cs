namespace ProjectGenerator.Templates;

public partial class TemplateProvider
{
    public string GetApplicationDependencyInjectionTemplate()
    {
        return $@"using Microsoft.Extensions.DependencyInjection;

namespace {_namespace}.Application;

public static class DependencyInjection
{{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {{
        // Add MediatR if using CQRS
        // services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Add FluentValidation if using
        // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        return services;
    }}
}}
";
    }

    public string GetInfrastructureDependencyInjectionTemplate()
    {
        return $@"using {_namespace}.Application.Interfaces;
using {_namespace}.Infrastructure.Data;
using {_namespace}.Infrastructure.Repositories;
using {_namespace}.SharedKernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {_namespace}.Infrastructure;

public static class DependencyInjection
{{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {{
        // Add DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString(""DefaultConnection""),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Add Repositories
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        
        // Add Services based on features (these will be generated)
        // services.AddScoped<IProductService, ProductService>();
        // services.AddScoped<IOrderService, OrderService>();
        // services.AddScoped<ICartService, CartService>();
        // services.AddScoped<IBlogService, BlogService>();
        // services.AddScoped<ICategoryService, CategoryService>();
        // services.AddScoped<IInvoiceService, InvoiceService>();

        return services;
    }}
}}
";
    }

    public string GetDbContextEnhancedTemplate()
    {
        return $@"using {_namespace}.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace {_namespace}.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {{
    }}

    // DbSets
    public DbSet<Product> Products {{ get; set; }}
    public DbSet<Category> Categories {{ get; set; }}
    public DbSet<ProductImage> ProductImages {{ get; set; }}
    public DbSet<Order> Orders {{ get; set; }}
    public DbSet<OrderItem> OrderItems {{ get; set; }}
    public DbSet<Invoice> Invoices {{ get; set; }}
    public DbSet<Cart> Carts {{ get; set; }}
    public DbSet<CartItem> CartItems {{ get; set; }}
    public DbSet<Blog> Blogs {{ get; set; }}
    public DbSet<BlogComment> BlogComments {{ get; set; }}
    public DbSet<BlogCategory> BlogCategories {{ get; set; }}

    protected override void OnModelCreating(ModelBuilder builder)
    {{
        base.OnModelCreating(builder);

        // Configure Identity tables
        builder.Entity<ApplicationUser>(entity =>
        {{
            entity.ToTable(""Users"");
        }});

        builder.Entity<ApplicationRole>(entity =>
        {{
            entity.ToTable(""Roles"");
        }});

        builder.Entity<IdentityUserRole<int>>(entity =>
        {{
            entity.ToTable(""UserRoles"");
        }});

        builder.Entity<IdentityUserClaim<int>>(entity =>
        {{
            entity.ToTable(""UserClaims"");
        }});

        builder.Entity<IdentityUserLogin<int>>(entity =>
        {{
            entity.ToTable(""UserLogins"");
        }});

        builder.Entity<IdentityRoleClaim<int>>(entity =>
        {{
            entity.ToTable(""RoleClaims"");
        }});

        builder.Entity<IdentityUserToken<int>>(entity =>
        {{
            entity.ToTable(""UserTokens"");
        }});

        // Configure domain entities
        ConfigureProductEntities(builder);
        ConfigureOrderEntities(builder);
        ConfigureCartEntities(builder);
        ConfigureBlogEntities(builder);
    }}

    private void ConfigureProductEntities(ModelBuilder builder)
    {{
        builder.Entity<Product>(entity =>
        {{
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType(""decimal(18,2)"");
            entity.Property(e => e.DiscountPrice).HasColumnType(""decimal(18,2)"");
            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }});

        builder.Entity<Category>(entity =>
        {{
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            
            entity.HasOne(e => e.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }});

        builder.Entity<ProductImage>(entity =>
        {{
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }});
    }}

    private void ConfigureOrderEntities(ModelBuilder builder)
    {{
        builder.Entity<Order>(entity =>
        {{
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasColumnType(""decimal(18,2)"");
            entity.Property(e => e.DiscountAmount).HasColumnType(""decimal(18,2)"");
            entity.Property(e => e.ShippingCost).HasColumnType(""decimal(18,2)"");
            entity.Property(e => e.FinalAmount).HasColumnType(""decimal(18,2)"");
        }});

        builder.Entity<OrderItem>(entity =>
        {{
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType(""decimal(18,2)"");
            entity.Property(e => e.TotalPrice).HasColumnType(""decimal(18,2)"");
            
            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }});

        builder.Entity<Invoice>(entity =>
        {{
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasColumnType(""decimal(18,2)"");
            entity.Property(e => e.PaidAmount).HasColumnType(""decimal(18,2)"");
            entity.Property(e => e.RemainingAmount).HasColumnType(""decimal(18,2)"");
            
            entity.HasOne(e => e.Order)
                .WithOne(o => o.Invoice)
                .HasForeignKey<Invoice>(e => e.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        }});
    }}

    private void ConfigureCartEntities(ModelBuilder builder)
    {{
        builder.Entity<Cart>(entity =>
        {{
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
        }});

        builder.Entity<CartItem>(entity =>
        {{
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(e => e.CartId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }});
    }}

    private void ConfigureBlogEntities(ModelBuilder builder)
    {{
        builder.Entity<Blog>(entity =>
        {{
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(250);
            entity.HasIndex(e => e.Slug).IsUnique();
        }});

        builder.Entity<BlogComment>(entity =>
        {{
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Blog)
                .WithMany(b => b.Comments)
                .HasForeignKey(e => e.BlogId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(e => e.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);
        }});

        builder.Entity<BlogCategory>(entity =>
        {{
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        }});
    }}
}}

public class ApplicationUser : IdentityUser<int>
{{
    public string? FirstName {{ get; set; }}
    public string? LastName {{ get; set; }}
    public DateTime? BirthDate {{ get; set; }}
    public bool IsActive {{ get; set; }} = true;
    public DateTime RegisterDate {{ get; set; }} = DateTime.UtcNow;
}}

public class ApplicationRole : IdentityRole<int>
{{
    public string? Description {{ get; set; }}
}}
";
    }
}
