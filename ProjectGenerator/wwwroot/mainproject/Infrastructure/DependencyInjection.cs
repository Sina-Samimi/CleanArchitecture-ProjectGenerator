using System;
using MobiRooz.Application.Interfaces;
using MobiRooz.Domain.Entities;
using MobiRooz.Infrastructure.Persistence;
using MobiRooz.Infrastructure.Persistence.Interceptors;
using MobiRooz.Infrastructure.Persistence.Repositories;
using MobiRooz.Infrastructure.Services;
using MobiRooz.Infrastructure.Services.Billing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace MobiRooz.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var auditInterceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
            // Add MultipleActiveResultSets=true to connection string to avoid DataReader conflicts
            var connectionStringWithMars = connectionString.Contains("MultipleActiveResultSets", StringComparison.OrdinalIgnoreCase)
                ? connectionString
                : connectionString.TrimEnd(';') + ";MultipleActiveResultSets=true;";
            options.UseSqlServer(connectionStringWithMars)
                .AddInterceptors(auditInterceptor)
                .ConfigureWarnings(warnings => 
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        // دیتابیس جداگانه برای لاگ‌ها
        var logsConnectionString = configuration.GetConnectionString("LogsConnection")
            ?? throw new InvalidOperationException("Connection string 'LogsConnection' was not found.");
        
        services.AddDbContext<LogsDbContext>((serviceProvider, options) =>
        {
            // Add MultipleActiveResultSets=true to connection string to avoid DataReader conflicts
            var connectionStringWithMars = logsConnectionString.Contains("MultipleActiveResultSets", StringComparison.OrdinalIgnoreCase)
                ? logsConnectionString
                : logsConnectionString.TrimEnd(';') + ";MultipleActiveResultSets=true;";
            options.UseSqlServer(connectionStringWithMars);
        });

        services.AddHttpContextAccessor();

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/PhoneLogin";
            options.AccessDeniedPath = "/Error/403";
            options.ReturnUrlParameter = "returnUrl";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(2);
            options.Cookie.MaxAge = TimeSpan.FromDays(2);
        });

        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.FromSeconds(30);
        });

                                                        services.AddScoped<IFormFileSettingServices, FormFileSettingServices>();
        services.AddScoped<IAccessPermissionRepository, AccessPermissionRepository>();
        services.AddScoped<IPageAccessPolicyRepository, PageAccessPolicyRepository>();
        services.AddScoped<IPermissionDefinitionService, PermissionDefinitionService>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IAuditContext, HttpAuditContext>();
        services.AddScoped<IApplicationLogRepository, ApplicationLogRepository>();
        services.AddScoped<IBlogRepository, BlogRepository>();
        services.AddScoped<IBlogCommentRepository, BlogCommentRepository>();
        services.AddScoped<IBlogCategoryRepository, BlogCategoryRepository>();
        services.AddScoped<IBlogAuthorRepository, BlogAuthorRepository>();
        services.AddScoped<ISiteCategoryRepository, SiteCategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductCommentRepository, ProductCommentRepository>();
        services.AddScoped<IProductViolationReportRepository, ProductViolationReportRepository>();
        services.AddScoped<IProductCustomRequestRepository, ProductCustomRequestRepository>();
        services.AddScoped<IProductRequestRepository, ProductRequestRepository>();
        services.AddScoped<IProductOfferRepository, ProductOfferRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ISellerProfileRepository, SellerProfileRepository>();
        services.AddScoped<IFinancialSettingRepository, FinancialSettingRepository>();
        services.AddScoped<ISiteSettingRepository, SiteSettingRepository>();
        services.AddScoped<IAboutSettingRepository, AboutSettingRepository>();
        services.AddScoped<ISmsSettingRepository, SmsSettingRepository>();
        services.AddScoped<IPaymentSettingRepository, PaymentSettingRepository>();
        services.AddScoped<IBannerRepository, BannerRepository>();
        services.AddScoped<IDeploymentProfileRepository, DeploymentProfileRepository>();
        services.AddScoped<INavigationMenuRepository, NavigationMenuRepository>();
        services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IProductBackInStockSubscriptionRepository, ProductBackInStockSubscriptionRepository>();
        services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IWithdrawalRequestRepository, WithdrawalRequestRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IVisitRepository, VisitRepository>();
        services.AddScoped<IShipmentTrackingRepository, ShipmentTrackingRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IContactMessageRepository, ContactMessageRepository>();
        services.AddScoped<IUserAddressRepository, UserAddressRepository>();
        services.AddScoped<IApplicationLogRepository, ApplicationLogRepository>();
        services.AddScoped<ISeoMetadataRepository, SeoMetadataRepository>();
        services.AddScoped<IPageFaqRepository, PageFaqRepository>();
        services.AddScoped<ISeoOgImageRepository, SeoOgImageRepository>();
        services.AddScoped<ISeoMetadataService, SeoMetadataService>();
        services.AddScoped<ISeoTemplateService, SeoTemplateService>();
        services.AddScoped<IAdminDashboardMetricsService, AdminDashboardMetricsService>();
        // IBankingGatewayService is kept for ConfirmBankInvoicePaymentCommand compatibility
        // But SimulatedBank is no longer used - all OnlineGateway payments go through PaymentController (Parbad/ZarinPal)
        services.AddSingleton<IBankingGatewayService, SimulatedBankingGatewayService>();
        // Register concrete SMS provider for direct resolution (used by Hangfire jobs)
        services.AddScoped<KavenegarSmsProvider>();
        // Default mapping for ISMSSenderService (can be overridden by Web project with a background-wrapper)
        services.AddScoped<ISMSSenderService, KavenegarSmsProvider>();

        return services;
    }
}
