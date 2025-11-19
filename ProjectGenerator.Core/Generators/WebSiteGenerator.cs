using ProjectGenerator.Core.Models;
using ProjectGenerator.Core.Templates;

namespace ProjectGenerator.Core.Generators;

public class WebSiteGenerator
{
    private readonly ProjectConfig _config;
    private readonly string _websitePath;
    private readonly WebSiteTemplates _templates;

    public WebSiteGenerator(ProjectConfig config, string websitePath)
    {
        _config = config;
        _websitePath = websitePath;
        _templates = new WebSiteTemplates(config.Namespace, config.ProjectName);
    }

    public void Generate()
    {
        Console.WriteLine("Generating WebSite layer with all features...");

        // Create basic structure
        CreateBasicStructure();

        // Generate Areas
        GenerateAdminArea();
        
        if (_config.Options.Features.SellerPanel)
        {
            GenerateSellerArea();
        }

        GenerateUserArea();

        // Generate Controllers
        GenerateControllers();

        // Generate Views
        GenerateSharedViews();

        Console.WriteLine("✓ WebSite layer generated successfully");
    }

    private void CreateBasicStructure()
    {
        var dirs = new[] 
        { 
            "Controllers", 
            "Views", 
            "Views/Shared",
            "Models", 
            "wwwroot/css", 
            "wwwroot/js",
            "wwwroot/images",
            "Areas", 
            "Services",
            "ViewComponents",
            "Authorization"
        };

        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(_websitePath, dir));
        }

        // Generate CSS files with theme settings
        GenerateThemeCss();
    }

    private void GenerateThemeCss()
    {
        var cssPath = Path.Combine(_websitePath, "wwwroot", "css");
        var theme = _config.Theme;

        // Generate main site.css with theme variables
        var siteCss = $@"/* Theme Variables */
:root {{
    --primary-color: {theme.PrimaryColor};
    --secondary-color: {theme.SecondaryColor};
    --success-color: {theme.SuccessColor};
    --danger-color: {theme.DangerColor};
    --warning-color: {theme.WarningColor};
    --info-color: {theme.InfoColor};
    --light-color: {theme.LightColor};
    --dark-color: {theme.DarkColor};
    --background-color: {theme.BackgroundColor};
    --text-color: {theme.TextColor};
    --font-family: {theme.FontFamily};
}}

* {{
    font-family: var(--font-family);
}}

body {{
    background-color: var(--background-color);
    color: var(--text-color);
    font-family: var(--font-family);
    direction: rtl;
    text-align: right;
    min-height: 100vh;
    display: flex;
    flex-direction: column;
}}

main {{
    flex: 1;
}}

/* Navbar Styles */
.navbar {{
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}}

.navbar-brand {{
    font-weight: 600;
    font-size: 1.25rem;
}}

.navbar-nav .nav-link {{
    padding: 0.5rem 1rem;
    transition: all 0.3s;
}}

.navbar-nav .nav-link:hover {{
    background-color: rgba(255,255,255,0.1);
    border-radius: 4px;
}}

/* Button Styles */
.btn {{
    border-radius: 6px;
    padding: 0.5rem 1.5rem;
    font-weight: 500;
    transition: all 0.3s;
}}

.btn:hover {{
    transform: translateY(-1px);
    box-shadow: 0 4px 8px rgba(0,0,0,0.2);
}}

/* Card Styles */
.card {{
    border: none;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    border-radius: 8px;
    transition: transform 0.2s, box-shadow 0.2s;
    margin-bottom: 1.5rem;
}}

.card:hover {{
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}}

.card-img-top {{
    border-radius: 8px 8px 0 0;
}}

/* Form Styles */
.form-control {{
    border-radius: 6px;
    border: 1px solid #ddd;
    padding: 0.75rem;
    transition: all 0.3s;
}}

.form-control:focus {{
    border-color: var(--primary-color);
    box-shadow: 0 0 0 0.2rem rgba(0,123,255,0.25);
}}

/* Table Styles */
.table {{
    background-color: white;
    border-radius: 8px;
    overflow: hidden;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}}

.table thead {{
    background-color: var(--primary-color);
    color: white;
}}

.table tbody tr {{
    transition: background-color 0.2s;
}}

.table tbody tr:hover {{
    background-color: #f8f9fa;
}}

/* Alert Styles */
.alert {{
    border-radius: 8px;
    border: none;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}}

/* Footer Styles */
footer {{
    margin-top: auto;
}}

footer a {{
    text-decoration: none;
    transition: color 0.3s;
}}

footer a:hover {{
    color: white !important;
}}

/* Hero Section */
.hero-section {{
    background: linear-gradient(135deg, var(--primary-color) 0%, var(--secondary-color) 100%);
    color: white;
    padding: 80px 0;
    text-align: center;
    margin-bottom: 3rem;
}}

/* Responsive */
@media (max-width: 768px) {{
    .hero-section {{
        padding: 40px 0;
    }}
    
    .card {{
        margin-bottom: 1rem;
    }}
}}
";

        File.WriteAllText(Path.Combine(cssPath, "site.css"), siteCss);

        // Generate admin.css with new design
        var adminCss = GenerateAdminPanelCss(theme);
        File.WriteAllText(Path.Combine(cssPath, "admin.css"), adminCss);

        // Generate seller.css with new design
        var sellerCss = GenerateSellerPanelCss(theme);
        File.WriteAllText(Path.Combine(cssPath, "seller.css"), sellerCss);

        // Generate user.css with new design
        var userCss = GenerateUserPanelCss(theme);
        File.WriteAllText(Path.Combine(cssPath, "user.css"), userCss);

        // Generate JavaScript files
        GenerateJavaScriptFiles();
    }

    private void GenerateJavaScriptFiles()
    {
        var jsPath = Path.Combine(_websitePath, "wwwroot", "js");

        // site.js
        var siteJs = @"// Main Site JavaScript
$(document).ready(function() {
    // Auto-hide alerts after 5 seconds
    setTimeout(function() {
        $('.alert').fadeOut('slow');
    }, 5000);
    
    // Form validation
    $('form').on('submit', function(e) {
        if (!this.checkValidity()) {
            e.preventDefault();
            e.stopPropagation();
        }
        $(this).addClass('was-validated');
    });
});
";
        File.WriteAllText(Path.Combine(jsPath, "site.js"), siteJs);

        // admin.js
        var adminJs = @"// Admin Panel JavaScript
$(document).ready(function() {
    // Auto-hide alerts
    setTimeout(function() {
        $('.alert').fadeOut('slow');
    }, 5000);
    
    // Confirm delete actions
    $('.btn-danger').on('click', function(e) {
        if (!confirm('آیا از حذف این مورد اطمینان دارید؟')) {
            e.preventDefault();
        }
    });
    
    // Sidebar active link
    var currentPath = window.location.pathname;
    $('.sidebar-nav a').each(function() {
        if ($(this).attr('href') === currentPath) {
            $(this).addClass('active');
        }
    });
});
";
        File.WriteAllText(Path.Combine(jsPath, "admin.js"), adminJs);

        // seller.js
        File.WriteAllText(Path.Combine(jsPath, "seller.js"), adminJs.Replace("Admin", "Seller"));

        // user.js
        File.WriteAllText(Path.Combine(jsPath, "user.js"), adminJs.Replace("Admin", "User"));
    }

    private string GenerateUserPanelCss(ThemeSettings theme)
    {
        return $@"/* User Panel Styles - Arsis Design */
:root {{
    --primary-color: {theme.PrimaryColor};
    --secondary-color: {theme.SecondaryColor};
    --success-color: {theme.SuccessColor};
    --info-color: {theme.InfoColor};
    --light-color: {theme.LightColor};
    --dark-color: {theme.DarkColor};
    --font-family: {theme.FontFamily};
}}

body {{
    font-family: var(--font-family);
    direction: rtl;
    background-color: #f5f7fa;
}}

.user-panel-wrapper {{
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    background-color: #f5f7fa;
}}

/* Top Header */
.user-top-header {{
    background-color: white;
    padding: 1rem 2rem;
    box-shadow: 0 2px 4px rgba(0,0,0,0.05);
    border-bottom: 1px solid #e0e0e0;
    position: sticky;
    top: 0;
    z-index: 100;
}}

.user-avatar-small {{
    width: 40px;
    height: 40px;
    border-radius: 50%;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: bold;
    font-size: 18px;
}}

.user-name-dropdown {{
    display: flex;
    align-items: center;
    cursor: pointer;
}}

.user-name-text {{
    font-weight: 600;
    color: #2c3e50;
}}

/* Content Wrapper */
.user-panel-content-wrapper {{
    display: flex;
    flex: 1;
    gap: 2rem;
    padding: 2rem;
    max-width: 1400px;
    margin: 0 auto;
    width: 100%;
}}

/* Right Sidebar */
.user-right-sidebar {{
    width: 320px;
    flex-shrink: 0;
}}

.sidebar-brand {{
    background: white;
    padding: 2rem;
    border-radius: 12px;
    text-align: center;
    margin-bottom: 1.5rem;
    box-shadow: 0 2px 8px rgba(0,0,0,0.05);
}}

.brand-logo {{
    width: 80px;
    height: 80px;
    border-radius: 12px;
    background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 36px;
    font-weight: bold;
    margin: 0 auto 1rem;
}}

.sidebar-brand h4 {{
    margin: 0;
    color: #2c3e50;
    font-weight: 600;
}}

.user-summary-card {{
    background: white;
    padding: 2rem;
    border-radius: 12px;
    text-align: center;
    margin-bottom: 1.5rem;
    box-shadow: 0 2px 8px rgba(0,0,0,0.05);
}}

.user-avatar-medium {{
    width: 100px;
    height: 100px;
    border-radius: 50%;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: bold;
    font-size: 36px;
    margin: 0 auto;
}}

.user-summary-card h5 {{
    color: #2c3e50;
    font-weight: 600;
    margin-top: 1rem;
}}

.user-status {{
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    margin-top: 1rem;
    color: #27ae60;
    font-size: 14px;
}}

.status-dot {{
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background-color: #27ae60;
    animation: pulse 2s infinite;
}}

@keyframes pulse {{
    0% {{ opacity: 1; }}
    50% {{ opacity: 0.5; }}
    100% {{ opacity: 1; }}
}}

.user-contact-info {{
    margin-top: 1rem;
    text-align: right;
}}

.contact-item {{
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.5rem;
    font-size: 14px;
    color: #666;
}}

.contact-item i {{
    color: #3498db;
    width: 20px;
}}

.sidebar-menu {{
    background: white;
    border-radius: 12px;
    padding: 1.5rem;
    margin-bottom: 1.5rem;
    box-shadow: 0 2px 8px rgba(0,0,0,0.05);
}}

.menu-header {{
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
    font-weight: 600;
    color: #2c3e50;
    cursor: pointer;
}}

.menu-list {{
    list-style: none;
    padding: 0;
    margin: 0;
}}

.menu-list li {{
    margin-bottom: 0.5rem;
}}

.menu-list a {{
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.75rem 1rem;
    color: #666;
    text-decoration: none;
    border-radius: 8px;
    transition: all 0.3s;
}}

.menu-list a:hover,
.menu-list a.active {{
    background-color: #f0f4f8;
    color: var(--primary-color);
}}

.sidebar-help {{
    background: white;
    border-radius: 12px;
    padding: 1.5rem;
    box-shadow: 0 2px 8px rgba(0,0,0,0.05);
}}

.sidebar-help h6 {{
    color: #2c3e50;
    font-weight: 600;
    margin-bottom: 0.5rem;
}}

/* Main Content */
.user-main-content {{
    flex: 1;
    min-width: 0;
}}

.welcome-header {{
    background: white;
    padding: 2rem;
    border-radius: 12px;
    margin-bottom: 2rem;
    box-shadow: 0 2px 8px rgba(0,0,0,0.05);
}}

.welcome-text {{
    color: #2c3e50;
    font-weight: 600;
    margin: 0;
    display: flex;
    align-items: center;
    gap: 0.5rem;
}}

.greeting-emoji {{
    font-size: 2rem;
}}

.user-avatar-large {{
    width: 120px;
    height: 120px;
    border-radius: 50%;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: bold;
    font-size: 48px;
}}

/* Profile Page */
.profile-page {{
    background: white;
    padding: 2rem;
    border-radius: 12px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.05);
}}

.page-header {{
    margin-bottom: 2rem;
}}

.page-title {{
    color: #2c3e50;
    font-weight: 600;
    margin-bottom: 0.5rem;
}}

.user-info-card {{
    background: linear-gradient(135deg, #e3f2fd 0%, #ffffff 100%);
    padding: 2rem;
    border-radius: 12px;
    margin-bottom: 2rem;
    position: relative;
    border: 1px solid #e0e0e0;
}}

.card-label {{
    position: absolute;
    top: 1rem;
    left: 1rem;
    background: var(--primary-color);
    color: white;
    padding: 0.25rem 0.75rem;
    border-radius: 6px;
    font-size: 12px;
    font-weight: 600;
}}

.user-info-content {{
    padding-right: 150px;
}}

.user-name {{
    color: #2c3e50;
    font-weight: 600;
    margin-bottom: 1rem;
}}

.user-contact-details {{
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}}

.contact-detail-item {{
    display: flex;
    align-items: center;
    gap: 0.75rem;
    color: #666;
}}

.contact-detail-item i {{
    color: var(--primary-color);
    width: 20px;
}}

.user-meta-info {{
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-top: 1rem;
}}

.meta-item {{
    display: flex;
    gap: 0.5rem;
    font-size: 14px;
}}

.meta-label {{
    color: #999;
}}

.meta-value {{
    color: #2c3e50;
    font-weight: 500;
}}

.user-avatar-card {{
    position: absolute;
    left: 2rem;
    top: 50%;
    transform: translateY(-50%);
}}

.avatar-circle-large {{
    width: 150px;
    height: 150px;
    border-radius: 50%;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: bold;
    font-size: 64px;
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}}

/* Action Buttons */
.action-buttons-row {{
    display: flex;
    gap: 1rem;
    margin-bottom: 2rem;
}}

.btn-edit-profile {{
    background: linear-gradient(135deg, #27ae60 0%, #2ecc71 100%);
    color: white;
    border: none;
    padding: 0.75rem 2rem;
    border-radius: 8px;
    font-weight: 500;
    text-decoration: none;
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    transition: all 0.3s;
}}

.btn-edit-profile:hover {{
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(39, 174, 96, 0.3);
    color: white;
}}

.btn-account-details {{
    background: linear-gradient(135deg, #3498db 0%, #2980b9 100%);
    color: white;
    border: none;
    padding: 0.75rem 2rem;
    border-radius: 8px;
    font-weight: 500;
    text-decoration: none;
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    transition: all 0.3s;
}}

.btn-account-details:hover {{
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(52, 152, 219, 0.3);
    color: white;
}}

/* Info Cards */
.info-cards-row {{
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 1.5rem;
    margin-bottom: 2rem;
}}

.info-card {{
    background: white;
    padding: 1.5rem;
    border-radius: 12px;
    border: 1px solid #e0e0e0;
    transition: all 0.3s;
}}

.info-card:hover {{
    box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    transform: translateY(-2px);
}}

.info-card-header {{
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 1rem;
}}

.info-card-header i {{
    color: var(--primary-color);
    font-size: 20px;
}}

.info-card-header h6 {{
    margin: 0;
    color: #2c3e50;
    font-weight: 600;
    font-size: 14px;
}}

.info-value {{
    font-size: 24px;
    font-weight: 600;
    color: #2c3e50;
    margin-bottom: 0.5rem;
}}

.info-description {{
    font-size: 12px;
    color: #999;
}}

/* Bottom Sections */
.bottom-sections-row {{
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 1.5rem;
    margin-top: 2rem;
}}

.update-info-section,
.profile-progress-section {{
    background: #f8f9fa;
    padding: 1.5rem;
    border-radius: 12px;
    border: 1px solid #e0e0e0;
}}

.update-info-section h5,
.profile-progress-section h5 {{
    color: #2c3e50;
    font-weight: 600;
    margin-bottom: 0.75rem;
}}

.progress-link {{
    color: #667eea;
    text-decoration: none;
    font-weight: 500;
    display: inline-block;
    margin-top: 0.5rem;
}}

.progress-link:hover {{
    text-decoration: underline;
}}

/* Responsive */
@media (max-width: 1200px) {{
    .user-panel-content-wrapper {{
        flex-direction: column;
    }}
    
    .user-right-sidebar {{
        width: 100%;
    }}
    
    .info-cards-row {{
        grid-template-columns: repeat(2, 1fr);
    }}
}}

@media (max-width: 768px) {{
    .info-cards-row {{
        grid-template-columns: 1fr;
    }}
    
    .bottom-sections-row {{
        grid-template-columns: 1fr;
    }}
    
    .action-buttons-row {{
        flex-direction: column;
    }}
    
    .user-info-content {{
        padding-right: 0;
    }}
    
    .user-avatar-card {{
        position: relative;
        left: auto;
        top: auto;
        transform: none;
        margin-top: 1rem;
    }}
}}
";
    }

    private string GenerateAdminPanelCss(ThemeSettings theme)
    {
        return GenerateUserPanelCss(theme)
            .Replace("user-panel-wrapper", "admin-panel-wrapper")
            .Replace("user-top-header", "admin-top-header")
            .Replace("user-avatar-small", "admin-avatar-small")
            .Replace("user-name-dropdown", "admin-name-dropdown")
            .Replace("user-name-text", "admin-name-text")
            .Replace("user-panel-content-wrapper", "admin-panel-content-wrapper")
            .Replace("user-right-sidebar", "admin-right-sidebar")
            .Replace("user-summary-card", "admin-summary-card")
            .Replace("user-avatar-medium", "admin-avatar-medium")
            .Replace("user-status", "admin-status")
            .Replace("user-contact-info", "admin-contact-info")
            .Replace("user-main-content", "admin-main-content")
            .Replace("user-avatar-large", "admin-avatar-large")
            .Replace("linear-gradient(135deg, #667eea 0%, #764ba2 100%)", "linear-gradient(135deg, #2c3e50 0%, #34495e 100%)")
            .Replace("linear-gradient(135deg, #4CAF50 0%, #45a049 100%)", "linear-gradient(135deg, #3498db 0%, #2980b9 100%)")
            + @"
.admin-logo {
    background: linear-gradient(135deg, #3498db 0%, #2980b9 100%) !important;
}
";
    }

    private string GenerateSellerPanelCss(ThemeSettings theme)
    {
        return GenerateUserPanelCss(theme)
            .Replace("user-panel-wrapper", "seller-panel-wrapper")
            .Replace("user-top-header", "seller-top-header")
            .Replace("user-avatar-small", "seller-avatar-small")
            .Replace("user-name-dropdown", "seller-name-dropdown")
            .Replace("user-name-text", "seller-name-text")
            .Replace("user-panel-content-wrapper", "seller-panel-content-wrapper")
            .Replace("user-right-sidebar", "seller-right-sidebar")
            .Replace("user-summary-card", "seller-summary-card")
            .Replace("user-avatar-medium", "seller-avatar-medium")
            .Replace("user-status", "seller-status")
            .Replace("user-contact-info", "seller-contact-info")
            .Replace("user-main-content", "seller-main-content")
            .Replace("user-avatar-large", "seller-avatar-large")
            .Replace("linear-gradient(135deg, #667eea 0%, #764ba2 100%)", "linear-gradient(135deg, #27ae60 0%, #2ecc71 100%)")
            .Replace("linear-gradient(135deg, #4CAF50 0%, #45a049 100%)", "linear-gradient(135deg, #27ae60 0%, #2ecc71 100%)")
            + @"
.seller-logo {
    background: linear-gradient(135deg, #27ae60 0%, #2ecc71 100%) !important;
}
";
    }

    private void GenerateAdminArea()
    {
        var adminPath = Path.Combine(_websitePath, "Areas", "Admin");
        
        // Create directory structure
        var dirs = new[]
        {
            "Controllers",
            "Views",
            "Views/Home",
            "Views/Users",
            "Views/Roles",
            "Views/Settings"
        };

        var features = _config.Options.Features;
        
        if (features.ProductCatalog)
        {
            dirs = dirs.Concat(new[] { "Views/Products", "Views/Categories" }).ToArray();
        }
        
        if (features.Invoicing)
        {
            dirs = dirs.Concat(new[] { "Views/Orders", "Views/Invoices" }).ToArray();
        }
        
        if (features.BlogSystem)
        {
            dirs = dirs.Concat(new[] { "Views/Blogs" }).ToArray();
        }

        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(adminPath, dir));
        }

        // Generate Controllers
        File.WriteAllText(
            Path.Combine(adminPath, "Controllers", "HomeController.cs"),
            _templates.GetAdminHomeControllerTemplate()
        );

        File.WriteAllText(
            Path.Combine(adminPath, "Controllers", "UsersController.cs"),
            _templates.GetAdminUsersControllerTemplate()
        );

        File.WriteAllText(
            Path.Combine(adminPath, "Controllers", "RolesController.cs"),
            _templates.GetAdminRolesControllerTemplate()
        );

        if (features.ProductCatalog)
        {
            File.WriteAllText(
                Path.Combine(adminPath, "Controllers", "ProductsController.cs"),
                _templates.GetAdminProductsControllerTemplate()
            );
            File.WriteAllText(
                Path.Combine(adminPath, "Controllers", "CategoriesController.cs"),
                _templates.GetAdminCategoriesControllerTemplate()
            );
        }

        if (features.Invoicing)
        {
            File.WriteAllText(
                Path.Combine(adminPath, "Controllers", "OrdersController.cs"),
                _templates.GetAdminOrdersControllerTemplate()
            );
        }

        if (features.BlogSystem)
        {
            File.WriteAllText(
                Path.Combine(adminPath, "Controllers", "BlogsController.cs"),
                _templates.GetAdminBlogsControllerTemplate()
            );
        }

        // Generate Views
        File.WriteAllText(
            Path.Combine(adminPath, "Views", "_ViewStart.cshtml"),
            _templates.GetAdminViewStartTemplate()
        );

        File.WriteAllText(
            Path.Combine(adminPath, "Views", "_ViewImports.cshtml"),
            _templates.GetViewImportsTemplate()
        );

        File.WriteAllText(
            Path.Combine(adminPath, "Views", "Home", "Index.cshtml"),
            _templates.GetAdminDashboardTemplate()
        );

        // Generate Admin Users Views
        var adminUsersViewsPath = Path.Combine(adminPath, "Views", "Users");
        Directory.CreateDirectory(adminUsersViewsPath);
        File.WriteAllText(
            Path.Combine(adminUsersViewsPath, "Index.cshtml"),
            _templates.GetAdminUsersIndexViewTemplate()
        );
        File.WriteAllText(
            Path.Combine(adminUsersViewsPath, "Create.cshtml"),
            _templates.GetAdminUsersCreateViewTemplate()
        );
        File.WriteAllText(
            Path.Combine(adminUsersViewsPath, "Edit.cshtml"),
            _templates.GetAdminUsersEditViewTemplate()
        );

        if (features.ProductCatalog)
        {
            // Generate Admin Products Views
            var adminProductsViewsPath = Path.Combine(adminPath, "Views", "Products");
            Directory.CreateDirectory(adminProductsViewsPath);
            File.WriteAllText(
                Path.Combine(adminProductsViewsPath, "Index.cshtml"),
                _templates.GetAdminProductsIndexViewTemplate()
            );
            File.WriteAllText(
                Path.Combine(adminProductsViewsPath, "Create.cshtml"),
                _templates.GetAdminProductsCreateViewTemplate()
            );
            File.WriteAllText(
                Path.Combine(adminProductsViewsPath, "Edit.cshtml"),
                _templates.GetAdminProductsEditViewTemplate()
            );

            // Generate Admin Categories Views
            var adminCategoriesViewsPath = Path.Combine(adminPath, "Views", "Categories");
            Directory.CreateDirectory(adminCategoriesViewsPath);
            File.WriteAllText(
                Path.Combine(adminCategoriesViewsPath, "Index.cshtml"),
                _templates.GetAdminCategoriesIndexViewTemplate()
            );
            File.WriteAllText(
                Path.Combine(adminCategoriesViewsPath, "Create.cshtml"),
                _templates.GetAdminCategoriesCreateViewTemplate()
            );
        }

        if (features.Invoicing)
        {
            // Generate Admin Orders Views
            var adminOrdersViewsPath = Path.Combine(adminPath, "Views", "Orders");
            Directory.CreateDirectory(adminOrdersViewsPath);
            File.WriteAllText(
                Path.Combine(adminOrdersViewsPath, "Index.cshtml"),
                _templates.GetAdminOrdersIndexViewTemplate()
            );
            File.WriteAllText(
                Path.Combine(adminOrdersViewsPath, "Details.cshtml"),
                _templates.GetAdminOrdersDetailsViewTemplate()
            );
        }

        if (features.BlogSystem)
        {
            // Generate Admin Blogs Views
            var adminBlogsViewsPath = Path.Combine(adminPath, "Views", "Blogs");
            Directory.CreateDirectory(adminBlogsViewsPath);
            File.WriteAllText(
                Path.Combine(adminBlogsViewsPath, "Index.cshtml"),
                _templates.GetAdminBlogsIndexViewTemplate()
            );
            File.WriteAllText(
                Path.Combine(adminBlogsViewsPath, "Create.cshtml"),
                _templates.GetAdminBlogsCreateViewTemplate()
            );
        }
    }

    private void GenerateSellerArea()
    {
        var sellerPath = Path.Combine(_websitePath, "Areas", "Seller");
        
        var dirs = new[]
        {
            "Controllers",
            "Views",
            "Views/Home",
            "Views/Products",
            "Views/Orders"
        };

        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(sellerPath, dir));
        }

        // Generate Controllers
        File.WriteAllText(
            Path.Combine(sellerPath, "Controllers", "HomeController.cs"),
            _templates.GetSellerHomeControllerTemplate()
        );

        File.WriteAllText(
            Path.Combine(sellerPath, "Controllers", "ProductsController.cs"),
            _templates.GetSellerProductsControllerTemplate()
        );

        File.WriteAllText(
            Path.Combine(sellerPath, "Controllers", "OrdersController.cs"),
            _templates.GetSellerOrdersControllerTemplate()
        );

        // Generate Views
        File.WriteAllText(
            Path.Combine(sellerPath, "Views", "_ViewStart.cshtml"),
            _templates.GetSellerViewStartTemplate()
        );

        File.WriteAllText(
            Path.Combine(sellerPath, "Views", "_ViewImports.cshtml"),
            _templates.GetViewImportsTemplate()
        );

        File.WriteAllText(
            Path.Combine(sellerPath, "Views", "Home", "Index.cshtml"),
            _templates.GetSellerDashboardTemplate()
        );
    }

    private void GenerateUserArea()
    {
        var userPath = Path.Combine(_websitePath, "Areas", "User");
        
        var dirs = new[]
        {
            "Controllers",
            "Views",
            "Views/Home",
            "Views/Profile",
            "Views/Orders"
        };

        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(userPath, dir));
        }

        // Generate Controllers
        File.WriteAllText(
            Path.Combine(userPath, "Controllers", "HomeController.cs"),
            _templates.GetUserHomeControllerTemplate()
        );

        File.WriteAllText(
            Path.Combine(userPath, "Controllers", "ProfileController.cs"),
            _templates.GetUserProfileControllerTemplate()
        );

        File.WriteAllText(
            Path.Combine(userPath, "Controllers", "OrdersController.cs"),
            _templates.GetUserOrdersControllerTemplate()
        );

        // Generate Views
        File.WriteAllText(
            Path.Combine(userPath, "Views", "_ViewStart.cshtml"),
            _templates.GetUserViewStartTemplate()
        );

        File.WriteAllText(
            Path.Combine(userPath, "Views", "_ViewImports.cshtml"),
            _templates.GetViewImportsTemplate()
        );

        File.WriteAllText(
            Path.Combine(userPath, "Views", "Home", "Index.cshtml"),
            _templates.GetUserDashboardTemplate()
        );

        // Generate Profile Views
        var profileViewsPath = Path.Combine(userPath, "Views", "Profile");
        Directory.CreateDirectory(profileViewsPath);
        File.WriteAllText(
            Path.Combine(profileViewsPath, "Index.cshtml"),
            _templates.GetUserProfileIndexViewTemplate()
        );
        File.WriteAllText(
            Path.Combine(profileViewsPath, "Edit.cshtml"),
            _templates.GetUserProfileEditViewTemplate()
        );
    }

    private void GenerateControllers()
    {
        var controllersPath = Path.Combine(_websitePath, "Controllers");

        // Always generate these
        File.WriteAllText(
            Path.Combine(controllersPath, "HomeController.cs"),
            _templates.GetHomeControllerTemplate()
        );

        File.WriteAllText(
            Path.Combine(controllersPath, "AccountController.cs"),
            _templates.GetAccountControllerTemplate()
        );

        var features = _config.Options.Features;

        if (features.ProductCatalog)
        {
            File.WriteAllText(
                Path.Combine(controllersPath, "ProductController.cs"),
                _templates.GetProductControllerTemplate()
            );
        }

        if (features.ShoppingCart)
        {
            File.WriteAllText(
                Path.Combine(controllersPath, "CartController.cs"),
                _templates.GetCartControllerTemplate()
            );
            File.WriteAllText(
                Path.Combine(controllersPath, "CheckoutController.cs"),
                _templates.GetCheckoutControllerTemplate()
            );
        }

        if (features.BlogSystem)
        {
            File.WriteAllText(
                Path.Combine(controllersPath, "BlogController.cs"),
                _templates.GetBlogControllerTemplate()
            );
        }
    }

    private void GenerateSharedViews()
    {
        var sharedPath = Path.Combine(_websitePath, "Views", "Shared");

        File.WriteAllText(
            Path.Combine(sharedPath, "_Layout.cshtml"),
            _templates.GetLayoutTemplate(_config.Theme)
        );

        File.WriteAllText(
            Path.Combine(sharedPath, "_AdminLayout.cshtml"),
            _templates.GetAdminLayoutTemplate()
        );

        if (_config.Options.Features.SellerPanel)
        {
            File.WriteAllText(
                Path.Combine(sharedPath, "_SellerLayout.cshtml"),
                _templates.GetSellerLayoutTemplate()
            );
        }

        File.WriteAllText(
            Path.Combine(sharedPath, "_UserLayout.cshtml"),
            _templates.GetUserLayoutTemplate()
        );

        // Generate ViewImports and ViewStart
        File.WriteAllText(
            Path.Combine(_websitePath, "Views", "_ViewImports.cshtml"),
            _templates.GetViewImportsTemplate()
        );

        File.WriteAllText(
            Path.Combine(_websitePath, "Views", "_ViewStart.cshtml"),
            _templates.GetMainViewStartTemplate()
        );

        // Generate Main Site Views
        GenerateMainSiteViews();

        // Generate ViewModels
        GenerateViewModels();
    }

    private void GenerateViewModels()
    {
        var modelsPath = Path.Combine(_websitePath, "Models");
        Directory.CreateDirectory(modelsPath);

        // Main site ViewModels
        File.WriteAllText(
            Path.Combine(modelsPath, "LoginViewModel.cs"),
            _templates.GetLoginViewModelTemplate()
        );
        File.WriteAllText(
            Path.Combine(modelsPath, "RegisterViewModel.cs"),
            _templates.GetRegisterViewModelTemplate()
        );

        // Admin Area ViewModels
        var adminModelsPath = Path.Combine(_websitePath, "Areas", "Admin", "Models");
        Directory.CreateDirectory(adminModelsPath);
        File.WriteAllText(
            Path.Combine(adminModelsPath, "CreateUserViewModel.cs"),
            _templates.GetCreateUserViewModelTemplate()
        );
        File.WriteAllText(
            Path.Combine(adminModelsPath, "EditUserViewModel.cs"),
            _templates.GetEditUserViewModelTemplate()
        );

        // User Area ViewModels
        var userModelsPath = Path.Combine(_websitePath, "Areas", "User", "Models");
        Directory.CreateDirectory(userModelsPath);
        File.WriteAllText(
            Path.Combine(userModelsPath, "ProfileEditViewModel.cs"),
            _templates.GetProfileEditViewModelTemplate()
        );
    }

    private void GenerateMainSiteViews()
    {
        var viewsPath = Path.Combine(_websitePath, "Views");
        var features = _config.Options.Features;

        // Home Views
        var homeViewsPath = Path.Combine(viewsPath, "Home");
        Directory.CreateDirectory(homeViewsPath);
        File.WriteAllText(
            Path.Combine(homeViewsPath, "Index.cshtml"),
            _templates.GetHomeIndexViewTemplate()
        );
        File.WriteAllText(
            Path.Combine(homeViewsPath, "About.cshtml"),
            _templates.GetHomeAboutViewTemplate()
        );
        File.WriteAllText(
            Path.Combine(homeViewsPath, "Contact.cshtml"),
            _templates.GetHomeContactViewTemplate()
        );

        // Account Views
        var accountViewsPath = Path.Combine(viewsPath, "Account");
        Directory.CreateDirectory(accountViewsPath);
        File.WriteAllText(
            Path.Combine(accountViewsPath, "Login.cshtml"),
            _templates.GetAccountLoginViewTemplate()
        );
        // Register view is no longer needed - registration is handled in Login flow

        if (features.ProductCatalog)
        {
            // Product Views
            var productViewsPath = Path.Combine(viewsPath, "Product");
            Directory.CreateDirectory(productViewsPath);
            File.WriteAllText(
                Path.Combine(productViewsPath, "Index.cshtml"),
                _templates.GetProductIndexViewTemplate()
            );
            File.WriteAllText(
                Path.Combine(productViewsPath, "Details.cshtml"),
                _templates.GetProductDetailsViewTemplate()
            );
        }

        if (features.ShoppingCart)
        {
            // Cart Views
            var cartViewsPath = Path.Combine(viewsPath, "Cart");
            Directory.CreateDirectory(cartViewsPath);
            File.WriteAllText(
                Path.Combine(cartViewsPath, "Index.cshtml"),
                _templates.GetCartIndexViewTemplate()
            );

            // Checkout Views
            var checkoutViewsPath = Path.Combine(viewsPath, "Checkout");
            Directory.CreateDirectory(checkoutViewsPath);
            File.WriteAllText(
                Path.Combine(checkoutViewsPath, "Index.cshtml"),
                _templates.GetCheckoutIndexViewTemplate()
            );
        }

        if (features.BlogSystem)
        {
            // Blog Views
            var blogViewsPath = Path.Combine(viewsPath, "Blog");
            Directory.CreateDirectory(blogViewsPath);
            File.WriteAllText(
                Path.Combine(blogViewsPath, "Index.cshtml"),
                _templates.GetBlogIndexViewTemplate()
            );
            File.WriteAllText(
                Path.Combine(blogViewsPath, "Details.cshtml"),
                _templates.GetBlogDetailsViewTemplate()
            );
        }
    }
}
