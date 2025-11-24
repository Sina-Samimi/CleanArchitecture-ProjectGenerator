using System.Net;
using Arsis.Domain.Base;
using Arsis.Domain.Entities;
using Arsis.Domain.Entities.Assessments;
using Arsis.Domain.Entities.Billing;
using Arsis.Domain.Entities.Blogs;
using Arsis.Domain.Entities.Catalog;
using Arsis.Domain.Entities.Discounts;
using Arsis.Domain.Entities.Navigation;
using Arsis.Domain.Entities.Orders;
using Arsis.Domain.Entities.Settings;
using Arsis.Domain.Entities.Teachers;
using Arsis.Domain.Entities.Tests;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Arsis.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Talent> Talents => Set<Talent>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<UserResponse> UserResponses => Set<UserResponse>();

    public DbSet<TalentScore> TalentScores => Set<TalentScore>();

    public DbSet<AccessPermission> AccessPermissions => Set<AccessPermission>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<PageAccessPolicy> PageAccessPolicies => Set<PageAccessPolicy>();

    public DbSet<Blog> Blogs => Set<Blog>();

    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();

    public DbSet<BlogAuthor> BlogAuthors => Set<BlogAuthor>();

    public DbSet<BlogComment> BlogComments => Set<BlogComment>();

    public DbSet<BlogDailyView> BlogViews => Set<BlogDailyView>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<ProductExecutionStep> ProductExecutionSteps => Set<ProductExecutionStep>();

    public DbSet<ProductFaq> ProductFaqs => Set<ProductFaq>();

    public DbSet<ProductComment> ProductComments => Set<ProductComment>();

    public DbSet<SiteCategory> SiteCategories => Set<SiteCategory>();

    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();

    public DbSet<FinancialSetting> FinancialSettings => Set<FinancialSetting>();

    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    public DbSet<DeploymentProfile> DeploymentProfiles => Set<DeploymentProfile>();

    public DbSet<NavigationMenuItem> NavigationMenuItems => Set<NavigationMenuItem>();

    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();

    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();

    public DbSet<ShoppingCartItem> ShoppingCartItems => Set<ShoppingCartItem>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    public DbSet<InvoiceItemAttribute> InvoiceItemAttributes => Set<InvoiceItemAttribute>();

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<WalletAccount> WalletAccounts => Set<WalletAccount>();

    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    public DbSet<Test> Tests => Set<Test>();

    public DbSet<TestQuestion> TestQuestions => Set<TestQuestion>();

    public DbSet<TestQuestionOption> TestQuestionOptions => Set<TestQuestionOption>();

    public DbSet<UserTestAttempt> UserTestAttempts => Set<UserTestAttempt>();

    public DbSet<UserTestAnswer> UserTestAnswers => Set<UserTestAnswer>();

    public DbSet<TestResult> TestResults => Set<TestResult>();

    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();

    public DbSet<AssessmentRun> AssessmentRuns => Set<AssessmentRun>();

    public DbSet<AssessmentUserResponse> AssessmentResponses => Set<AssessmentUserResponse>();

    public DbSet<Organization> Organizations => Set<Organization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureBaseEntities(modelBuilder);

        modelBuilder.Entity<AssessmentQuestion>()
            .HasIndex(q => new { q.TestType, q.Index })
            .IsUnique();

        modelBuilder.Entity<AssessmentUserResponse>()
            .HasIndex(r => new { r.AssessmentRunId, r.AssessmentQuestionId })
            .IsUnique();

        modelBuilder.Entity<AssessmentUserResponse>()
            .HasOne(r => r.Run)
            .WithMany(r => r.Responses)
            .HasForeignKey(r => r.AssessmentRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssessmentUserResponse>()
            .HasOne(r => r.Question)
            .WithMany(q => q.Responses)
            .HasForeignKey(r => r.AssessmentQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApplicationUser>().HasData(SeedData.SystemUser);
        modelBuilder.Entity<Talent>().HasData(SeedData.Talents);
        modelBuilder.Entity<Question>().HasData(SeedData.Questions);
    }

    private static void ConfigureBaseEntities(ModelBuilder modelBuilder)
    {
        var ipConverter = new ValueConverter<IPAddress, string>(
            address => address.ToString(),
            value => string.IsNullOrWhiteSpace(value) ? IPAddress.None : IPAddress.Parse(value));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || !typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var entityBuilder = modelBuilder.Entity(entityType.ClrType);

            entityBuilder.Property<IPAddress>(nameof(BaseEntity.Ip))
                .HasConversion(ipConverter)
                .HasMaxLength(64)
                .HasColumnName("Ip")
                .IsConcurrencyToken(false);

            entityBuilder.Property<string>(nameof(BaseEntity.CreatorId))
                .IsRequired()
                .HasMaxLength(450);

            entityBuilder.Property<string?>(nameof(BaseEntity.UpdaterId))
                .HasMaxLength(450)
                .IsConcurrencyToken(false);

            entityBuilder.Property<DateTimeOffset>(nameof(BaseEntity.CreateDate));
            
            entityBuilder.Property<DateTimeOffset>(nameof(BaseEntity.UpdateDate))
                .IsConcurrencyToken(false);
            
            entityBuilder.Property<DateTimeOffset?>(nameof(BaseEntity.RemoveDate));
            entityBuilder.Property<bool>(nameof(BaseEntity.IsDeleted));

            entityBuilder.HasOne(typeof(ApplicationUser), nameof(BaseEntity.Creator))
                .WithMany()
                .HasForeignKey(nameof(BaseEntity.CreatorId))
                .OnDelete(DeleteBehavior.Restrict);

            entityBuilder.HasOne(typeof(ApplicationUser), nameof(BaseEntity.Updater))
                .WithMany()
                .HasForeignKey(nameof(BaseEntity.UpdaterId))
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
