using System.Text;
using Arsis.Application;
using Arsis.Application.Assessments;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Assessments;
using Arsis.Infrastructure;
using Arsis.Infrastructure.Persistence;
using Arsis.SharedKernel.Authorization;
using EndPoint.WebSite.Authorization;
using EndPoint.WebSite.Extensions;
using EndPoint.WebSite.Services;
using EndPoint.WebSite.Services.Cart;
using EndPoint.WebSite.Services.Blog;
using EndPoint.WebSite.Services.Products;
using EndPoint.WebSite.Growth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

Console.OutputEncoding = Encoding.UTF8; 

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<MatricesOptions>(builder.Configuration.GetSection("Matrices"));
builder.Services.Configure<ScoringOptions>(builder.Configuration.GetSection("Scoring"));

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var env = sp.GetRequiredService<IHostEnvironment>();
    return MatrixLoader.Load(configuration, env.ContentRootPath);
});

builder.Services.AddScoped<IQuestionImporter, QuestionImporter>();
builder.Services.AddScoped<EndPoint.WebSite.Growth.AssessmentService>();
builder.Services.AddScoped<EndPoint.WebSite.App.IJobGroupLabelsProvider, EndPoint.WebSite.App.JobGroupLabelsProvider>();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AdminPagePermissionFilter>();
})
.ConfigureApiBehaviorOptions(options =>
{
    // تنظیم پیام‌های خطای فارسی برای validation
    options.InvalidModelStateResponseFactory = context =>
    {
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(context.ModelState);
    };
})
.AddDataAnnotationsLocalization(options =>
{
    // فارسی‌سازی پیام‌های خطای پیش‌فرض DataAnnotations
    options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(Program));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Assessment API",
        Version = "v1",
        Description = "Clifton + Schwartz assessment endpoints"
    });
});
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IPhoneVerificationService, PhoneVerificationService>();
builder.Services.AddSingleton<ISmsSender, LoggingSmsSender>();
builder.Services.AddScoped<AdminPagePermissionFilter>();
builder.Services.AddScoped<IPageDescriptorProvider, MvcPageDescriptorProvider>();
builder.Services.AddScoped<IPageAccessCache, PageAccessCache>();
builder.Services.AddScoped<IBlogService, DatabaseBlogService>();
builder.Services.AddScoped<IProductCatalogService, DatabaseProductCatalogService>();
builder.Services.AddScoped<ICartCookieService, CartCookieService>();
builder.Services.AddScoped<IPageDescriptorProvider, MvcPageDescriptorProvider>();

// Register custom authorization policy provider for permission-based authorization
builder.Services.AddSingleton<IAuthorizationPolicyProvider, Arsis.SharedKernel.Authorization.PermissionPolicyProvider>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.TeacherPanelAccess, policy =>
    {
        policy.Requirements.Add(new TeacherPanelRequirement());
    });
});

// Register authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, TeacherPanelAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, Arsis.SharedKernel.Authorization.PermissionAuthorizationHandler>();

// Register HttpContextAccessor for tag helpers
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var importer = scope.ServiceProvider.GetRequiredService<IQuestionImporter>();

    var hasClifton = await dbContext.AssessmentQuestions.AnyAsync(q => q.TestType == AssessmentTestType.Clifton);
    var hasPvq = await dbContext.AssessmentQuestions.AnyAsync(q => q.TestType == AssessmentTestType.Pvq);

    if (!hasClifton)
    {
        await importer.ImportCliftonAsync(string.Empty, CancellationToken.None);
    }

    if (!hasPvq)
    {
        await importer.ImportPvqAsync(string.Empty, CancellationToken.None);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Assessment API v1");
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "product-list",
    pattern: "Product",
    defaults: new { controller = "Product", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//await app.ApplyDatabaseMigrationsAsync();
//await app.SeedAdminUserAsync();

app.Run();
