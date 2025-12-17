using System.Data.Common;
using System.Globalization;
using System.Linq;
using TestAttarClone.Domain.Entities;
using TestAttarClone.Domain.Entities.Blogs;
using TestAttarClone.Domain.Entities.Catalog;
using TestAttarClone.Domain.Entities.Navigation;
using TestAttarClone.Domain.Entities.Settings;
using TestAttarClone.Domain.Enums;
using TestAttarClone.Infrastructure.Persistence;
using TestAttarClone.SharedKernel.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace TestAttarClone.WebSite.Extensions;

internal static class WebApplicationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigrator");

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        try
        {
            // Ensure database exists first
            await EnsureDatabaseExistsAsync(connectionString, logger);

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

            if (pendingMigrations.Count > 0)
            {
                logger.LogInformation(
                    "Applying {MigrationCount} pending database migrations: {Migrations}",
                    pendingMigrations.Count,
                    string.Join(", ", pendingMigrations));
            }

            // MigrateAsync will create the database if it doesn't exist, but we're doing it explicitly above for better error handling
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations completed successfully.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while applying database migrations.");
            throw;
        }
    }

    private static async Task EnsureDatabaseExistsAsync(string connectionString, ILogger logger)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = builder.InitialCatalog;

            if (string.IsNullOrEmpty(databaseName))
            {
                logger.LogWarning("No database name specified in connection string.");
                return;
            }

            // Connect to master database to create the target database
            builder.InitialCatalog = "master";
            var masterConnectionString = builder.ConnectionString;

            using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{databaseName}')
                BEGIN
                    CREATE DATABASE [{databaseName}]
                    SELECT 'Database created' AS Result
                END
                ELSE
                BEGIN
                    SELECT 'Database already exists' AS Result
                END";

            var result = await command.ExecuteScalarAsync();
            logger.LogInformation("Database check: {Result}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error ensuring database exists. Will attempt to continue with migration...");
            // Don't throw - let MigrateAsync handle it
        }
    }

    public static async Task SeedAdminUserAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("AdminUserSeeder");

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var section = configuration.GetSection("SeedAdminUser");
        if (!section.Exists())
        {
            logger.LogInformation("SeedAdminUser section not found. Skipping admin user seeding.");
            return;
        }

        var email = section["Email"]?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("SeedAdminUser.Email is missing. Skipping admin user seeding.");
            return;
        }

        var password = section["Password"];
        var passwordHash = section["PasswordHash"];
        if (string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(passwordHash))
        {
            logger.LogWarning("SeedAdminUser requires either Password or PasswordHash. Skipping admin user seeding.");
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is null)
        {
            await CreateUserAsync(userManager, logger, section, email, password, passwordHash);
        }
        else
        {
            await UpdateUserAsync(userManager, logger, section, existingUser, password, passwordHash);
        }
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        IConfiguration section,
        string email,
        string? password,
        string? passwordHash)
    {
        var configuredId = TryParseGuid(section["Id"]);
        if (configuredId is Guid existingId)
        {
            var existingUser = await userManager.FindByIdAsync(existingId.ToString());
            if (existingUser is not null)
            {
                logger.LogWarning(
                    "Skipping admin user creation for '{RequestedEmail}' because the configured Id {UserId} is already assigned to '{ExistingEmail}'.",
                    email,
                    existingId,
                    existingUser.Email);
                return;
            }
        }

        var createdOn = TryParseDateTime(section["CreatedOn"]) ?? DateTimeOffset.UtcNow;
        var requestedLastModified = TryParseDateTime(section["LastModifiedOn"]);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            FullName = ResolveFullName(section, "Arsis Platform Admin"),
            EmailConfirmed = TryParseBool(section["EmailConfirmed"]) ?? true,
            IsActive = TryParseBool(section["IsActive"]) ?? true,
            IsDeleted = TryParseBool(section["IsDeleted"]) ?? false,
            CreatedOn = createdOn,
            LastModifiedOn = requestedLastModified ?? createdOn,
            DeactivatedOn = TryParseDateTime(section["DeactivatedOn"]),
            DeactivationReason = ResolveOptionalString(section["DeactivationReason"]),
            DeletedOn = TryParseDateTime(section["DeletedOn"])
        };

        IdentityResult result;
        if (!string.IsNullOrWhiteSpace(password))
        {
            result = await userManager.CreateAsync(user, password);
        }
        else
        {
            user.PasswordHash = passwordHash;
            result = await userManager.CreateAsync(user);
        }

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create admin user '{email}': {string.Join(", ", result.Errors.Select(error => error.Description))}");
        }

        logger.LogInformation("Created admin user '{Email}' from configuration.", email);
    }

    private static async Task UpdateUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        IConfiguration section,
        ApplicationUser user,
        string? password,
        string? passwordHash)
    {
        var hasChanges = false;
        var requestedLastModified = TryParseDateTime(section["LastModifiedOn"]);

        var fullName = ResolveFullName(section, user.FullName);
        if (!string.Equals(user.FullName, fullName, StringComparison.Ordinal))
        {
            user.FullName = fullName;
            hasChanges = true;
        }

        var emailConfirmed = TryParseBool(section["EmailConfirmed"]);
        if (emailConfirmed.HasValue && user.EmailConfirmed != emailConfirmed.Value)
        {
            user.EmailConfirmed = emailConfirmed.Value;
            hasChanges = true;
        }

        var isActive = TryParseBool(section["IsActive"]);
        if (isActive.HasValue && user.IsActive != isActive.Value)
        {
            user.IsActive = isActive.Value;
            hasChanges = true;
        }

        var deactivatedOn = TryParseDateTime(section["DeactivatedOn"]);
        if (user.DeactivatedOn != deactivatedOn)
        {
            user.DeactivatedOn = deactivatedOn;
            hasChanges = true;
        }

        var deactivationReason = ResolveOptionalString(section["DeactivationReason"]);
        if (!string.Equals(user.DeactivationReason, deactivationReason, StringComparison.Ordinal))
        {
            user.DeactivationReason = deactivationReason;
            hasChanges = true;
        }

        var isDeleted = TryParseBool(section["IsDeleted"]);
        if (isDeleted.HasValue && user.IsDeleted != isDeleted.Value)
        {
            user.IsDeleted = isDeleted.Value;
            hasChanges = true;
        }

        var deletedOn = TryParseDateTime(section["DeletedOn"]);
        if (user.DeletedOn != deletedOn)
        {
            user.DeletedOn = deletedOn;
            hasChanges = true;
        }

        if (requestedLastModified.HasValue && user.LastModifiedOn != requestedLastModified.Value)
        {
            user.LastModifiedOn = requestedLastModified.Value;
            hasChanges = true;
        }
        else if (hasChanges)
        {
            user.LastModifiedOn = DateTimeOffset.UtcNow;
        }

        if (hasChanges)
        {
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to update admin user '{user.Email}': {string.Join(", ", updateResult.Errors.Select(error => error.Description))}");
            }

            logger.LogInformation("Updated admin user '{Email}' from configuration.", user.Email);
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, resetToken, password);
            if (!resetResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to reset admin user password for '{user.Email}': {string.Join(", ", resetResult.Errors.Select(error => error.Description))}");
            }

            logger.LogInformation("Reset admin user password for '{Email}' from configuration.", user.Email);
        }
        else if (!string.IsNullOrWhiteSpace(passwordHash) && user.PasswordHash != passwordHash)
        {
            user.PasswordHash = passwordHash;
            var passwordUpdateResult = await userManager.UpdateAsync(user);
            if (!passwordUpdateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to update admin user password hash for '{user.Email}': {string.Join(", ", passwordUpdateResult.Errors.Select(error => error.Description))}");
            }

            logger.LogInformation("Updated admin user password hash for '{Email}' from configuration.", user.Email);
        }
    }

    private static string ResolveFullName(IConfiguration configuration, string fallback)
    {
        var value = configuration["FullName"];
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? ResolveOptionalString(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Guid? TryParseGuid(string? value)
        => Guid.TryParse(value, out var result) ? result : null;

    private static bool? TryParseBool(string? value)
        => bool.TryParse(value, out var result) ? result : null;

    private static DateTimeOffset? TryParseDateTime(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result)
            ? result
            : null;

    /// <summary>
    /// Seeds the default roles (Admin, User, Seller, Author) if they don't exist
    /// </summary>
    public static async Task SeedRolesAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RoleSeeder");

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var roles = new[]
        {
            RoleNames.Admin,
            RoleNames.User,
            RoleNames.Seller,
            RoleNames.Author
        };

        foreach (var roleName in roles)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                var role = new IdentityRole(roleName)
                {
                    NormalizedName = roleName.ToUpperInvariant()
                };

                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    logger.LogInformation("Created role '{RoleName}'", roleName);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create role '{RoleName}': {Errors}", roleName, errors);
                }
            }
            else
            {
                logger.LogDebug("Role '{RoleName}' already exists, skipping", roleName);
            }
        }
    }

    /// <summary>
    /// Seeds initial data for the application (categories, products, blogs, menus, settings, etc.)
    /// </summary>
    public static async Task SeedInitialDataAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("InitialDataSeeder");

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        try
        {
            // Check if data already exists
            if (await dbContext.SiteCategories.AnyAsync() || await dbContext.Users.AnyAsync(u => u.Email == "samimisina72@gmail.com"))
            {
                logger.LogInformation("Initial data already exists, skipping seed.");
                return;
            }

            var systemUserId = TestAttarClone.Domain.Constants.SystemUsers.AutomationId;
            var now = DateTimeOffset.UtcNow;
            var ipAddress = System.Net.IPAddress.Parse("127.0.0.1");

            // 1. Create initial user
            logger.LogInformation("Creating initial user...");
            var initialUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "samimisina72@gmail.com",
                NormalizedUserName = "SAMIMISINA72@GMAIL.COM",
                Email = "samimisina72@gmail.com",
                NormalizedEmail = "SAMIMISINA72@GMAIL.COM",
                EmailConfirmed = true,
                PhoneNumber = "09927277290",
                PhoneNumberConfirmed = true,
                FullName = "سینا صمیمی",
                IsActive = true,
                CreatedOn = now,
                LastModifiedOn = now,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            var passwordHasher = new PasswordHasher<ApplicationUser>();
            initialUser.PasswordHash = passwordHasher.HashPassword(initialUser, "Admin@123456");

            await userManager.CreateAsync(initialUser);
            await userManager.AddToRolesAsync(initialUser, new[] { RoleNames.Admin, RoleNames.User });

            // 2. Create Categories with Subcategories
            logger.LogInformation("Creating categories...");
            var categories = new List<SiteCategory>();

            var categoryData = new[]
            {
                ("موبایل", "mobile", new[] { "گوشی هوشمند", "گوشی معمولی", "لوازم جانبی موبایل" }),
                ("لپ تاپ", "laptop", new[] { "لپ تاپ گیمینگ", "لپ تاپ اداری", "لپ تاپ دانشجویی" }),
                ("لوازم برقی", "electrical", new[] { "یخچال", "ماشین لباسشویی", "اجاق گاز" }),
                ("آرایشی بهداشتی", "cosmetics", new[] { "لوازم آرایشی", "محصولات مراقبت پوست", "عطر و ادکلن" }),
                ("مد و پوشاک", "fashion", new[] { "لباس مردانه", "لباس زنانه", "کفش" })
            };

            foreach (var (name, slug, subcategories) in categoryData)
            {
                var category = new SiteCategory(
                    name, slug, CategoryScope.Product, 
                    $"دسته بندی {name}", null, null)
                {
                    CreatorId = systemUserId,
                    CreateDate = now,
                    UpdateDate = now,
                    Ip = ipAddress
                };
                categories.Add(category);
                dbContext.Add(category);

                foreach (var subName in subcategories)
                {
                    var subSlug = subName.Replace(' ', '-').ToLowerInvariant();
                    var subCategory = new SiteCategory(
                        subName, subSlug, CategoryScope.Product,
                        $"زیر دسته {subName}", category, null)
                    {
                        CreatorId = systemUserId,
                        CreateDate = now,
                        UpdateDate = now,
                        Ip = ipAddress
                    };
                    dbContext.Add(subCategory);
                }
            }

            await dbContext.SaveChangesAsync();

            // 3. Create Products
            logger.LogInformation("Creating products...");
            var productImages = new[]
            {
                "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=500",
                "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=500",
                "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=500",
                "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=500",
                "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=500"
            };

            for (int i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                var product = new Product(
                    "سایت", 
                    "محصول نمونه برای دسته بندی",
                    "این یک محصول نمونه است که برای نمایش در سایت ایجاد شده است.",
                    ProductType.Physical,
                    100000m,
                    120000m,
                    true,
                    10,
                    category,
                    $"محصول {category.Name}",
                    "توضیحات سئو",
                    "کلمات کلیدی",
                    $"product-{category.Slug}",
                    "index, follow",
                    productImages[i],
                    null,
                    null,
                    true,
                    now)
                {
                    CreatorId = systemUserId,
                    CreateDate = now,
                    UpdateDate = now,
                    Ip = ipAddress
                };
                dbContext.Add(product);
            }

            await dbContext.SaveChangesAsync();

            // 4. Create Banner
            logger.LogInformation("Creating banner...");
            var banner = new Banner(
                "فروشگاه آنلاین",
                "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=1200",
                "/product",
                "بنر فروشگاه",
                0,
                true,
                null,
                null,
                true)
            {
                CreatorId = systemUserId,
                CreateDate = now,
                UpdateDate = now,
                Ip = ipAddress
            };
            dbContext.Add(banner);
            await dbContext.SaveChangesAsync();

            // 5. Create Blog Author and assign Author role
            logger.LogInformation("Creating blog author...");
            var blogAuthor = new BlogAuthor(
                initialUser.FullName,
                "نویسنده سایت",
                null,
                true,
                initialUser.Id)
            {
                CreatorId = systemUserId,
                CreateDate = now,
                UpdateDate = now,
                Ip = ipAddress
            };
            dbContext.Add(blogAuthor);
            await dbContext.SaveChangesAsync();

            await userManager.AddToRoleAsync(initialUser, RoleNames.Author);

            // 6. Create Blog Categories and Blogs
            logger.LogInformation("Creating blog categories and blogs...");
            var blogCategoryNames = new[] { "مقالات", "اخبار", "آموزش" };
            var blogCategories = new List<BlogCategory>();

            foreach (var catName in blogCategoryNames)
            {
                var blogCategory = new BlogCategory(
                    catName,
                    catName.Replace(' ', '-').ToLowerInvariant(),
                    $"دسته بندی {catName}")
                {
                    CreatorId = systemUserId,
                    CreateDate = now,
                    UpdateDate = now,
                    Ip = ipAddress
                };
                blogCategories.Add(blogCategory);
                dbContext.Add(blogCategory);
            }

            await dbContext.SaveChangesAsync();

            var blogTitles = new[] { "مقاله نمونه", "خبر جدید", "آموزش کاربردی" };
            var blogSummaries = new[] { "خلاصه مقاله", "خلاصه خبر", "خلاصه آموزش" };
            var blogContents = new[] 
            { 
                "<p>این یک مقاله نمونه است.</p>", 
                "<p>این یک خبر جدید است.</p>", 
                "<p>این یک آموزش کاربردی است.</p>" 
            };

            for (int i = 0; i < blogCategories.Count; i++)
            {
                var blogCategory = blogCategories[i];
                var blog = new Blog(
                    blogTitles[i],
                    blogSummaries[i],
                    blogContents[i],
                    blogCategory,
                    blogAuthor,
                    BlogStatus.Published,
                    5,
                    blogTitles[i],
                    blogSummaries[i],
                    "کلمات کلیدی",
                    $"blog-{blogCategory.Slug}",
                    "index, follow",
                    "https://images.unsplash.com/photo-1499750310107-5fef28a66643?w=800",
                    null,
                    now)
                {
                    CreatorId = systemUserId,
                    CreateDate = now,
                    UpdateDate = now,
                    Ip = ipAddress
                };
                dbContext.Add(blog);
            }

            await dbContext.SaveChangesAsync();

            // 7. Create Menu Items
            logger.LogInformation("Creating menu items...");
            var menuItems = new[]
            {
                ("خانه", "/", "", 1),
                ("محصولات", "/product", "", 2),
                ("تماس با ما", "/contactus", "", 3),
                ("درباره ما", "/aboutus", "", 4)
            };

            foreach (var (title, url, icon, order) in menuItems)
            {
                var menuItem = new NavigationMenuItem(
                    title, url, icon, true, false, order, null, null)
                {
                    CreatorId = systemUserId,
                    CreateDate = now,
                    UpdateDate = now,
                    Ip = ipAddress
                };
                dbContext.Add(menuItem);
            }

            await dbContext.SaveChangesAsync();

            // 8. Create Site Settings
            logger.LogInformation("Creating site settings...");
            var siteSetting = new SiteSetting(
                "عطاری آنلاین",
                "info@attar.com",
                "support@attar.com",
                "02112345678",
                "02187654321",
                "تهران، ایران",
                "تماس با ما",
                null,
                null,
                "توضیحات کوتاه سایت",
                null,
                null,
                null,
                null,
                null,
                null,
                false)
            {
                CreatorId = systemUserId,
                CreateDate = now,
                UpdateDate = now,
                Ip = ipAddress
            };
            dbContext.Add(siteSetting);
            await dbContext.SaveChangesAsync();

            // 9. Create Financial Settings
            logger.LogInformation("Creating financial settings...");
            var financialSetting = new FinancialSetting(
                70m, // Seller share
                9m,  // VAT
                10m, // Platform commission
                5m,  // Affiliate commission
                PlatformCommissionCalculationMethod.DeductFromSeller)
            {
                CreatorId = systemUserId,
                CreateDate = now,
                UpdateDate = now,
                Ip = ipAddress
            };
            dbContext.Add(financialSetting);
            await dbContext.SaveChangesAsync();

            // 10. Create About Settings
            logger.LogInformation("Creating about settings...");
            var aboutSetting = new AboutSetting(
                "درباره ما",
                "عطاری آنلاین ما با بیش از ۲۰ سال تجربه در زمینه فروش گیاهان دارویی و محصولات طبیعی، همواره تلاش کرده است تا بهترین و باکیفیت‌ترین محصولات را در اختیار مشتریان عزیز قرار دهد.",
                "چشم‌انداز ما ارائه بهترین خدمات و محصولات به مشتریان است.",
                "ماموریت ما بهبود سلامت و رفاه مشتریان از طریق محصولات طبیعی است.",
                null,
                "درباره ما - عطاری آنلاین",
                "درباره عطاری آنلاین - بیش از ۲۰ سال تجربه")
            {
                CreatorId = systemUserId,
                CreateDate = now,
                UpdateDate = now,
                Ip = ipAddress
            };
            dbContext.Add(aboutSetting);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Initial data seeded successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding initial data.");
            throw;
        }
    }

    /// <summary>
    /// ایجاد خودکار Schema و جدول لاگ در دیتابیس (در صورت عدم وجود)
    /// </summary>
    public static async Task EnsureLogsDatabaseCreatedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("LogsDatabaseInitializer");

        try
        {
            var logsDbContext = scope.ServiceProvider.GetRequiredService<LogsDbContext>();
            var connection = logsDbContext.Database.GetDbConnection();
            
            // ابتدا دیتابیس را ایجاد کن (اگر وجود نداشته باشد)
            var databaseName = GetDatabaseNameFromConnectionString(connection.ConnectionString);
            if (!string.IsNullOrEmpty(databaseName))
            {
                var masterConnectionString = GetMasterConnectionString(connection.ConnectionString, databaseName);
                await EnsureDatabaseExistsAsync(masterConnectionString, databaseName, logger);
            }
            
            await connection.OpenAsync();

            using var command = connection.CreateCommand();

            // چک کردن و ایجاد Schema
            command.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Logs')
                BEGIN
                    EXEC('CREATE SCHEMA Logs')
                    SELECT 'Schema created'
                END
                ELSE
                BEGIN
                    SELECT 'Schema already exists'
                END";

            var schemaResult = await command.ExecuteScalarAsync();
            logger.LogInformation("Logs schema check: {Result}", schemaResult);

            // چک کردن وجود جدول
            command.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.tables t 
                               INNER JOIN sys.schemas s ON t.schema_id = s.schema_id 
                               WHERE t.name = 'AttarApplicationLogs' AND s.name = 'Logs')
                BEGIN
                    -- ایجاد جدول AttarApplicationLogs
                    CREATE TABLE [Logs].[AttarApplicationLogs] (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [Level] nvarchar(50) NOT NULL,
                        [Message] nvarchar(max) NOT NULL,
                        [Exception] nvarchar(max) NULL,
                        [SourceContext] nvarchar(500) NULL,
                        [Properties] nvarchar(max) NULL,
                        [RequestPath] nvarchar(1000) NULL,
                        [RequestMethod] nvarchar(10) NULL,
                        [StatusCode] int NULL,
                        [ElapsedMs] float NULL,
                        [UserAgent] nvarchar(500) NULL,
                        [RemoteIpAddress] nvarchar(64) NULL,
                        [ApplicationName] nvarchar(200) NULL,
                        [MachineName] nvarchar(200) NULL,
                        [Environment] nvarchar(50) NULL,
                        [Ip] nvarchar(64) NOT NULL,
                        [CreatorId] nvarchar(450) NOT NULL,
                        [UpdaterId] nvarchar(450) NULL,
                        [CreateDate] datetimeoffset NOT NULL,
                        [UpdateDate] datetimeoffset NOT NULL,
                        [IsDeleted] bit NOT NULL,
                        [RemoveDate] datetimeoffset NULL
                    );

                    -- ایجاد Indexes برای Performance بهتر
                    CREATE INDEX IX_AttarApplicationLogs_Level ON [Logs].[AttarApplicationLogs]([Level]);
                    CREATE INDEX IX_AttarApplicationLogs_CreateDate ON [Logs].[AttarApplicationLogs]([CreateDate]);
                    CREATE INDEX IX_AttarApplicationLogs_Level_CreateDate ON [Logs].[AttarApplicationLogs]([Level], [CreateDate]);
                    CREATE INDEX IX_AttarApplicationLogs_ApplicationName ON [Logs].[AttarApplicationLogs]([ApplicationName]);
                    CREATE INDEX IX_AttarApplicationLogs_SourceContext ON [Logs].[AttarApplicationLogs]([SourceContext]);

                    SELECT 'Table created'
                END
                ELSE
                BEGIN
                    SELECT 'Table already exists'
                END";

            var tableResult = await command.ExecuteScalarAsync();
            logger.LogInformation("AttarApplicationLogs table check: {Result}", tableResult);

            await connection.CloseAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while ensuring logs database is created.");
            // خطا را throw نمی‌کنیم تا اپلیکیشن بتواند ادامه دهد
        }
    }

    private static async Task EnsureDatabaseExistsAsync(string masterConnectionString, string databaseName, ILogger logger)
    {
        try
        {
            using var masterConnection = new Microsoft.Data.SqlClient.SqlConnection(masterConnectionString);
            await masterConnection.OpenAsync();

            using var command = masterConnection.CreateCommand();
            command.CommandText = $@"
                IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{databaseName}')
                BEGIN
                    CREATE DATABASE [{databaseName}]
                    SELECT 'Database created'
                END
                ELSE
                BEGIN
                    SELECT 'Database already exists'
                END";

            var result = await command.ExecuteScalarAsync();
            logger.LogInformation("Logs database check: {Result}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating logs database '{DatabaseName}'", databaseName);
            throw;
        }
    }

    private static string GetDatabaseNameFromConnectionString(string connectionString)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        return builder.InitialCatalog;
    }

    private static string GetMasterConnectionString(string connectionString, string databaseName)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        builder.InitialCatalog = "master";
        return builder.ConnectionString;
    }
}
