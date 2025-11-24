using System.Globalization;
using System.Linq;
using Arsis.Domain.Entities;
using Arsis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace EndPoint.WebSite.Extensions;

internal static class WebApplicationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigrator");

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

            if (pendingMigrations.Count > 0)
            {
                logger.LogInformation(
                    "Applying {MigrationCount} pending database migrations: {Migrations}",
                    pendingMigrations.Count,
                    string.Join(", ", pendingMigrations));
            }

            await dbContext.Database.MigrateAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while applying database migrations.");
            throw;
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
}
