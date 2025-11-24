using System;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities;
using Arsis.Infrastructure.Persistence;
using Arsis.Infrastructure.Persistence.Interceptors;
using Arsis.Infrastructure.Persistence.Repositories;
using Arsis.Infrastructure.Services;
using Arsis.Infrastructure.Services.Billing;
using Arsis.Infrastructure.Services.Assessments;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Arsis.Application.Assessments;

namespace Arsis.Infrastructure;

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
            options.UseSqlServer(connectionString)
                .AddInterceptors(auditInterceptor);
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

        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<ITalentRepository, TalentRepository>();
        services.AddScoped<ITestSubmissionRepository, TestSubmissionRepository>();
        services.AddScoped<ITalentScoreRepository, TalentScoreRepository>();
        services.AddScoped<ITalentScoreCalculator, TalentScoreCalculator>();
        services.AddScoped<IReportGenerator, ReportGenerator>();
        services.AddScoped<IFormFileSettingServices, FormFileSettingServices>();
        services.AddScoped<IAccessPermissionRepository, AccessPermissionRepository>();
        services.AddScoped<IPageAccessPolicyRepository, PageAccessPolicyRepository>();
        services.AddScoped<IPermissionDefinitionService, PermissionDefinitionService>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IAuditContext, HttpAuditContext>();
        services.AddScoped<IBlogRepository, BlogRepository>();
        services.AddScoped<IBlogCommentRepository, BlogCommentRepository>();
        services.AddScoped<IBlogCategoryRepository, BlogCategoryRepository>();
        services.AddScoped<IBlogAuthorRepository, BlogAuthorRepository>();
        services.AddScoped<ISiteCategoryRepository, SiteCategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductCommentRepository, ProductCommentRepository>();
        services.AddScoped<ITeacherProfileRepository, TeacherProfileRepository>();
        services.AddScoped<IFinancialSettingRepository, FinancialSettingRepository>();
        services.AddScoped<ISiteSettingRepository, SiteSettingRepository>();
        services.AddScoped<IDeploymentProfileRepository, DeploymentProfileRepository>();
        services.AddScoped<INavigationMenuRepository, NavigationMenuRepository>();
        services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
        services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ITestRepository, TestRepository>();
        services.AddScoped<IUserTestAttemptRepository, UserTestAttemptRepository>();
        services.AddScoped<ITestResultRepository, TestResultRepository>();
        services.AddScoped<IAssessmentQuestionRepository, AssessmentQuestionRepository>();
        services.AddScoped<IAdminDashboardMetricsService, AdminDashboardMetricsService>();
        services.AddSingleton<MatrixLoader>();
        services.AddSingleton<CorrelationMatrixProvider>();
        services.AddSingleton<IScoringStrategy, MeanOverMaxStrategy>();
        services.AddSingleton<IScoringStrategy, WeightedRatioStrategy>();
        services.AddSingleton<IScoringStrategyResolver, ScoringStrategyResolver>();
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IOrganizationAnalysisService, OrganizationAnalysisService>();
        services.AddSingleton<IBankingGatewayService, SimulatedBankingGatewayService>();

        return services;
    }
}
