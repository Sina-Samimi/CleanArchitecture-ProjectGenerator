using System.Text;
using Attar.Application;
using Attar.Application.Interfaces;
using Attar.Infrastructure;
using Attar.Infrastructure.Persistence;
using Attar.SharedKernel.Authorization;
using Attar.WebSite.Authorization;
using Attar.WebSite.Extensions;
using Attar.WebSite.Services;
using Attar.WebSite.Services.Cart;
using Attar.WebSite.Services.Blog;
using Attar.WebSite.Services.Products;
using Attar.WebSite.Services.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using WebMarkupMin.AspNetCore7;
using Parbad.Builder;
using Parbad.Gateway.ZarinPal;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Data.SqlClient;

Console.OutputEncoding = Encoding.UTF8; 

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    var applicationName = context.Configuration["ApplicationName"] ?? "Attar";
    var environmentName = context.HostingEnvironment.EnvironmentName;
    
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", applicationName)
        .Enrich.WithProperty("ApplicationName", applicationName)
        .Enrich.WithProperty("ProjectName", applicationName)
        .Enrich.WithProperty("Environment", environmentName);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Configure Parbad Payment Gateway
// Account configuration will be loaded from database at runtime
// For now, using initial configuration - will be updated when payment settings are configured
// Configure Parbad Payment Gateway
// Load merchant settings from database when available; fall back to in-memory defaults
var merchantId = string.Empty;
var isSandbox = true;

// Try to read current payment settings from database (repository registered by AddInfrastructure)
try
{
    using var tempSp = builder.Services.BuildServiceProvider();
    using var scope = tempSp.CreateScope();
    var paymentRepo = scope.ServiceProvider.GetService<Attar.Application.Interfaces.IPaymentSettingRepository>();
    if (paymentRepo is not null)
    {
        var setting = paymentRepo.GetCurrentAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult();
        if (setting is not null)
        {
            merchantId = setting.ZarinPalMerchantId ?? string.Empty;
            isSandbox = setting.ZarinPalIsSandbox;
        }
    }
}
catch
{
    // Ignore startup failure - use defaults
}

builder.Services.AddParbad()
    .ConfigureGateways(gateways =>
    {
        gateways
            .AddZarinPal()
            .WithAccounts(options =>
            {
                options.AddInMemory(op =>
                {
                    op.MerchantId = merchantId;
                    op.IsSandbox = isSandbox;
                });
            });
    })
    .ConfigureStorage(storage =>
    {
        storage.UseMemoryCache();
    });

// Register Custom Log Sink (Singleton - only Error/Warning will be logged to DB)
// باید قبل از UseSerilog ثبت شود تا در UseSerilog در دسترس باشد
builder.Services.AddSingleton<Attar.Infrastructure.Services.Logging.AttarApplicationLogSink>();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AdminPagePermissionFilter>();
    options.Filters.Add<Attar.WebSite.Filters.SiteNameActionFilter>();
    options.Filters.Add<Attar.WebSite.Filters.VisitTrackingActionFilter>();
    options.Filters.Add<Attar.WebSite.Filters.SeoMetadataActionFilter>();
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
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IPhoneVerificationService, PhoneVerificationService>();
builder.Services.AddScoped<ISmsSender, HangfireSmsSender>();
// تمام ارسال‌های پیامکی سطح Application از طریق Hangfire انجام می‌شود
builder.Services.AddScoped<Attar.Application.Interfaces.ISMSSenderService, HangfireApplicationSmsSender>();
builder.Services.AddScoped<AdminPagePermissionFilter>();
builder.Services.AddScoped<Attar.WebSite.Filters.SiteNameActionFilter>();
builder.Services.AddScoped<Attar.WebSite.Filters.VisitTrackingActionFilter>();
builder.Services.AddScoped<Attar.WebSite.Filters.SeoMetadataActionFilter>();
builder.Services.AddScoped<Attar.WebSite.Filters.SeoMetadataActionFilter>();
builder.Services.AddScoped<IPageDescriptorProvider, MvcPageDescriptorProvider>();
builder.Services.AddScoped<IPageAccessCache, PageAccessCache>();
builder.Services.AddScoped<IBlogService, DatabaseBlogService>();
builder.Services.AddScoped<IProductCatalogService, DatabaseProductCatalogService>();
builder.Services.AddScoped<ICartCookieService, CartCookieService>();
builder.Services.AddScoped<ISessionCookieService, SessionCookieService>();
builder.Services.AddScoped<IPageDescriptorProvider, MvcPageDescriptorProvider>();

// Register custom authorization policy provider for permission-based authorization
builder.Services.AddSingleton<IAuthorizationPolicyProvider, Attar.SharedKernel.Authorization.PermissionPolicyProvider>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.SellerPanelAccess, policy =>
    {
        policy.Requirements.Add(new SellerPanelRequirement());
    });
});

// Register authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, SellerPanelAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, Attar.SharedKernel.Authorization.PermissionAuthorizationHandler>();

// Register HttpContextAccessor for tag helpers
builder.Services.AddHttpContextAccessor();

// تنظیمات Routing - همه URLها با حروف کوچک
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// افزودن فشرده‌سازی پاسخ (Gzip و Brotli)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/javascript",
        "application/json",
        "application/xml",
        "text/css",
        "text/html",
        "text/json",
        "text/plain",
        "text/xml",
        "image/svg+xml"
    });
});

// Hangfire configuration for background jobs (SMS, Email, etc.)
// پیش از راه‌اندازی Hangfire، مطمئن می‌شویم دیتابیس مخصوص آن (HangfireConnection) وجود دارد
EnsureHangfireDatabase(builder);

builder.Services.AddHangfire(configuration =>
{
    var hangfireConnectionString =
        builder.Configuration.GetConnectionString("HangfireConnection") ??
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Hangfire connection string is not configured.");

    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(hangfireConnectionString, new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        });
});

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = $"Attar-WebSite-Hangfire-{Environment.MachineName}";
});

// تنظیمات Brotli (فشرده‌سازی بهتر از Gzip)
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

// تنظیمات Gzip (برای مرورگرهای قدیمی‌تر)
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

// افزودن HTML Minification
builder.Services.AddWebMarkupMin(options =>
{
    options.AllowMinificationInDevelopmentEnvironment = false; // در Development غیرفعال
    options.AllowCompressionInDevelopmentEnvironment = false;
})
.AddHtmlMinification(options =>
{
    // تنظیمات پایه
    options.MinificationSettings.RemoveOptionalEndTags = false; // برای حفظ ساختار HTML
    options.MinificationSettings.RemoveEmptyAttributes = true;
    options.MinificationSettings.RemoveRedundantAttributes = true;
    
    // تنظیمات CSS و JavaScript
    options.MinificationSettings.RemoveCdataSectionsFromScriptsAndStyles = true;
    options.MinificationSettings.MinifyEmbeddedCssCode = true;
    options.MinificationSettings.MinifyEmbeddedJsCode = true;
    options.MinificationSettings.MinifyInlineCssCode = true;
    options.MinificationSettings.MinifyInlineJsCode = true;
    
    // حفظ حروف بزرگ/کوچک برای فارسی
    options.MinificationSettings.PreserveCase = true;
})
.AddHttpCompression();

var app = builder.Build();

// اضافه کردن Custom Sink به Serilog بعد از Build
// این Sink فقط Error/Warning را در دیتابیس می‌نویسد (Batch Writing برای Performance بهتر)
var customSink = app.Services.GetRequiredService<Attar.Infrastructure.Services.Logging.AttarApplicationLogSink>();

// Reconfigure Logger با Custom Sink
// این کار Logger قبلی را با یک Logger جدید که Custom Sink دارد جایگزین می‌کند
var applicationName = app.Configuration["ApplicationName"] ?? "Attar";
var environmentName = app.Environment.EnvironmentName;

// بستن Logger قبلی
Log.CloseAndFlush();

var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(app.Configuration)
    .ReadFrom.Services(app.Services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", applicationName)
    .Enrich.WithProperty("ApplicationName", applicationName)
    .Enrich.WithProperty("ProjectName", applicationName)
    .Enrich.WithProperty("Environment", environmentName)
    .WriteTo.Sink(customSink, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning);

Log.Logger = loggerConfig.CreateLogger();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    app.UseHsts();
}

// افزودن Response Compression Middleware (باید قبل از UseStaticFiles باشد)
app.UseResponseCompression();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});
app.UseHttpsRedirection();
app.UseStaticFiles();

// افزودن HTML Minification Middleware (باید بعد از UseStaticFiles و قبل از UseRouting باشد)
app.UseWebMarkupMin();

app.UseRouting();

// Maintenance mode middleware: when enabled, non-admin users see the maintenance page
app.UseMiddleware<Attar.WebSite.Middleware.MaintenanceModeMiddleware>();

// Add status code pages for error handling (400 is handled by form validation, so we skip it)
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    var statusCode = response.StatusCode;
    var path = context.HttpContext.Request.Path.Value ?? string.Empty;

    // Skip 400 - handled by form validation
    if (statusCode == 400)
    {
        return;
    }

    // Check if request is in User area
    var isUserArea = path.StartsWith("/User/", StringComparison.OrdinalIgnoreCase);

    string errorPath = statusCode switch
    {
        403 => isUserArea ? "/User/Error/403" : "/Error/403",
        404 => isUserArea ? "/User/Error/404" : "/Error/404",
        500 => isUserArea ? "/User/Error/500" : "/Error/500",
        _ => isUserArea ? "/User/Error/500" : "/Error/500"
    };

    response.Redirect(errorPath);
});

app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard (restricted to admins)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
});

// Local helper to ensure Hangfire database exists
static void EnsureHangfireDatabase(WebApplicationBuilder webAppBuilder)
{
    try
    {
        var connectionString = webAppBuilder.Configuration.GetConnectionString("HangfireConnection")
                             ?? webAppBuilder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return;
        }

        // برای ایجاد دیتابیس باید به master وصل شویم
        builder.InitialCatalog = "master";
        var masterConnectionString = builder.ConnectionString;

        using var connection = new SqlConnection(masterConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
IF DB_ID(@dbName) IS NULL
BEGIN
    CREATE DATABASE [" + databaseName + @"];
END";
        command.Parameters.AddWithValue("@dbName", databaseName);
        command.ExecuteNonQuery();
    }
    catch
    {
        // در صورت خطا (مثلاً نداشتن دسترسی ساخت دیتابیس)، برنامه را از کار نمی‌اندازیم
        // لاگ‌کردن جزئیات را می‌توان در صورت نیاز اضافه کرد
    }
}

// Add session validation middleware (must be after authentication)
app.UseMiddleware<Attar.WebSite.Middleware.SessionValidationMiddleware>();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "product-list",
    pattern: "Product",
    defaults: new { controller = "Product", action = "Index" });

app.MapControllerRoute(
    name: "about-us",
    pattern: "aboutus",
    defaults: new { controller = "Home", action = "About" });

app.MapControllerRoute(
    name: "contact-us",
    pattern: "contactus",
    defaults: new { controller = "Home", action = "Contact" });

app.MapControllerRoute(
    name: "dynamic-page",
    pattern: "Page/{slug}",
    defaults: new { controller = "Page", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// اعمال Migration ها و ایجاد دیتابیس
await app.ApplyDatabaseMigrationsAsync();

// Seed کردن نقش‌ها
await app.SeedRolesAsync();

// Seed کردن داده‌های اولیه (کاربر، دسته‌بندی‌ها، محصولات، بلاگ‌ها، منوها، تنظیمات)
await app.SeedInitialDataAsync();

//await app.SeedAdminUserAsync();

// ایجاد خودکار Schema و جدول لاگ در صورت عدم وجود
await app.EnsureLogsDatabaseCreatedAsync();

app.Run();
