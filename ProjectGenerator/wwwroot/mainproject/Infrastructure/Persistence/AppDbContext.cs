using System.Net;
using MobiRooz.Domain.Base;
using MobiRooz.Domain.Entities;
using MobiRooz.Domain.Entities.Billing;
using MobiRooz.Domain.Entities.Blogs;
using MobiRooz.Domain.Entities.Catalog;
using MobiRooz.Domain.Entities.Contacts;
using MobiRooz.Domain.Entities.Discounts;
using MobiRooz.Domain.Entities.Navigation;
using MobiRooz.Domain.Entities.Orders;
using MobiRooz.Domain.Entities.Notifications;
using MobiRooz.Domain.Entities.Settings;
using MobiRooz.Domain.Entities.Sellers;
using MobiRooz.Domain.Entities.Visits;
using MobiRooz.Domain.Entities.Tickets;
using MobiRooz.Domain.Entities.Seo;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.Identity;

namespace MobiRooz.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

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

    public DbSet<Wishlist> Wishlists => Set<Wishlist>();

    public DbSet<ProductBackInStockSubscription> ProductBackInStockSubscriptions => Set<ProductBackInStockSubscription>();

    public DbSet<ProductExecutionStep> ProductExecutionSteps => Set<ProductExecutionStep>();

    public DbSet<ProductFaq> ProductFaqs => Set<ProductFaq>();

    public DbSet<ProductComment> ProductComments => Set<ProductComment>();

    public DbSet<ProductViolationReport> ProductViolationReports => Set<ProductViolationReport>();

    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();

    public DbSet<ProductVariantAttribute> ProductVariantAttributes => Set<ProductVariantAttribute>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductVariantOption> ProductVariantOptions => Set<ProductVariantOption>();

    public DbSet<ProductCustomRequest> ProductCustomRequests => Set<ProductCustomRequest>();

    public DbSet<ProductRequest> ProductRequests => Set<ProductRequest>();

    public DbSet<ProductRequestImage> ProductRequestImages => Set<ProductRequestImage>();

    public DbSet<ProductOffer> ProductOffers => Set<ProductOffer>();

    public DbSet<SiteCategory> SiteCategories => Set<SiteCategory>();

    public DbSet<SellerProfile> SellerProfiles => Set<SellerProfile>();

    public DbSet<FinancialSetting> FinancialSettings => Set<FinancialSetting>();

    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    public DbSet<AboutSetting> AboutSettings => Set<AboutSetting>();

    public DbSet<SmsSetting> SmsSettings => Set<SmsSetting>();

    public DbSet<PaymentSetting> PaymentSettings => Set<PaymentSetting>();

    public DbSet<Banner> Banners => Set<Banner>();

    public DbSet<DeploymentProfile> DeploymentProfiles => Set<DeploymentProfile>();

    public DbSet<Domain.Entities.Pages.Page> Pages => Set<Domain.Entities.Pages.Page>();

    public DbSet<SiteVisit> SiteVisits => Set<SiteVisit>();

    public DbSet<PageVisit> PageVisits => Set<PageVisit>();

    public DbSet<ProductVisit> ProductVisits => Set<ProductVisit>();

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

    public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();

    public DbSet<ShipmentTracking> ShipmentTrackings => Set<ShipmentTracking>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketReply> TicketReplies => Set<TicketReply>();

    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();

    public DbSet<SeoMetadata> SeoMetadata => Set<SeoMetadata>();

    public DbSet<PageFaq> PageFaqs => Set<PageFaq>();

    public DbSet<SeoOgImage> SeoOgImages => Set<SeoOgImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureBaseEntities(modelBuilder);

        // Seed data is handled in SeedRolesAsync and SeedInitialDataAsync methods
        // to avoid duplicate key errors when running migrations multiple times
        // modelBuilder.Entity<ApplicationUser>().HasData(SeedData.SystemUser);
        // modelBuilder.Entity<IdentityRole>().HasData(SeedData.Roles);
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
