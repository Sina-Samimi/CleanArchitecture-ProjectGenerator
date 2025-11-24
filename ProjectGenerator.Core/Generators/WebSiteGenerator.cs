using System.Collections.Generic;
using ProjectGenerator.Core.Models;
using ProjectGenerator.Core.Templates;
using ProjectGenerator.Core.Utilities;

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
            "Authorization",
            "Properties"
        };

        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(_websitePath, dir));
        }

        // Generate launchSettings.json
        GenerateLaunchSettings();

        // Copy wwwroot files from template
        CopyWwwrootFiles();
        
        // Generate CSS files with theme settings
        GenerateThemeCss();
        
        // Generate ViewComponents
        GenerateViewComponents();
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
.landing-hero {{
    background: linear-gradient(120deg, var(--primary-color) 0%, var(--secondary-color) 100%);
    color: #fff;
    padding: 80px 0;
    position: relative;
    overflow: hidden;
}}

.landing-hero::after {{
    content: "";
    position: absolute;
    inset: 0;
    background: radial-gradient(circle at 20% 20%, rgba(255,255,255,0.12), transparent 40%),
                radial-gradient(circle at 80% 0%, rgba(255,255,255,0.1), transparent 35%);
    pointer-events: none;
}}

.hero-content {{
    position: relative;
    z-index: 1;
}}

.hero-title {{
    font-weight: 800;
    font-size: 36px;
    line-height: 1.4;
}}

.hero-subtitle {{
    color: rgba(255,255,255,0.85);
    font-size: 16px;
    margin: 1rem 0 1.5rem;
}}

.hero-actions {{
    display: flex;
    flex-wrap: wrap;
    gap: 0.75rem;
    margin-top: 1.5rem;
}}

.glass-card {{
    background: rgba(255,255,255,0.08);
    border: 1px solid rgba(255,255,255,0.2);
    border-radius: 16px;
    padding: 1.5rem;
    box-shadow: 0 20px 60px rgba(0,0,0,0.15);
    backdrop-filter: blur(8px);
}}

.stats-badges {{
    display: grid;
    grid-template-columns: repeat(3, minmax(140px, 1fr));
    gap: 0.75rem;
    margin-top: 1.5rem;
}}

.stats-badge {{
    background: rgba(255,255,255,0.12);
    border-radius: 12px;
    padding: 0.85rem 1rem;
    display: flex;
    align-items: center;
    gap: 0.75rem;
    color: #fff;
}}

.stats-badge i {{
    font-size: 18px;
    opacity: 0.9;
}}

.stats-badge .value {{
    font-weight: 700;
    font-size: 18px;
}}

.panel-card {{
    background: #fff;
    border-radius: 14px;
    padding: 1.25rem;
    border: 1px solid #e8e8e8;
    box-shadow: 0 10px 40px rgba(0,0,0,0.08);
    transition: all 0.3s;
}}

.panel-card:hover {{
    transform: translateY(-4px);
    box-shadow: 0 15px 50px rgba(0,0,0,0.12);
}}

.panel-card .panel-meta {{
    color: #6c757d;
    font-size: 13px;
    margin-top: 0.35rem;
}}

.panel-card .panel-link {{
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    color: var(--primary-color);
    text-decoration: none;
    font-weight: 600;
}}

.section-header {{
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 1rem;
}}

.section-title {{
    font-weight: 700;
    font-size: 22px;
    color: #2c3e50;
    margin: 0;
}}

.section-subtitle {{
    color: #6c757d;
    margin: 0;
}}

.feature-grid {{
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
    gap: 1rem;
}}

.feature-card {{
    border-radius: 14px;
    padding: 1.25rem;
    background: #fff;
    border: 1px solid #e9ecef;
    box-shadow: 0 10px 30px rgba(0,0,0,0.05);
    transition: all 0.3s;
}}

.feature-card:hover {{
    transform: translateY(-4px);
    box-shadow: 0 14px 40px rgba(0,0,0,0.08);
}}

.feature-icon {{
    width: 48px;
    height: 48px;
    border-radius: 12px;
    background: rgba(0,0,0,0.05);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    color: var(--primary-color);
    font-size: 20px;
    margin-bottom: 0.75rem;
}}

.cta-section {{
    background: linear-gradient(120deg, rgba(102,126,234,0.1), rgba(118,75,162,0.1));
    border: 1px solid #e8e8e8;
    padding: 2rem;
    border-radius: 16px;
    box-shadow: 0 12px 32px rgba(0,0,0,0.06);
}}

.cta-badges {{
    display: flex;
    gap: 0.75rem;
    flex-wrap: wrap;
    margin-top: 1rem;
}}

.cta-badge {{
    background: #fff;
    border: 1px solid #e6e6e6;
    padding: 0.5rem 0.75rem;
    border-radius: 10px;
    font-weight: 600;
    color: #555;
}}

/* Responsive */
@media (max-width: 992px) {{
    .stats-badges {{
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }}
}}

@media (max-width: 768px) {{
    .landing-hero {{
        padding: 48px 0;
    }}

    .stats-badges {{
        grid-template-columns: 1fr;
    }}

    .feature-grid {{
        grid-template-columns: 1fr;
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

        // Reapply original CSS to preserve full rule set
        CopyCssFromTemplate();

        var themeVars = $@":root {{
    --primary-color: {theme.PrimaryColor};
    --secondary-color: {theme.SecondaryColor};
    --font-family: {theme.FontFamily};
}}";
        File.WriteAllText(Path.Combine(cssPath, "theme-vars.css"), themeVars);

        // Generate JavaScript files
        GenerateJavaScriptFiles();
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
    background: transparent;
}}

.profile-shell {{
    display: grid;
    grid-template-columns: minmax(0, 2fr) minmax(280px, 1fr);
    gap: 1rem;
}}

.profile-hero {{
    background: linear-gradient(135deg, #e0f2fe 0%, #f8fbff 100%);
    border-radius: 16px;
    padding: 1.5rem;
    border: 1px solid #e5efff;
    box-shadow: 0 12px 30px rgba(15, 23, 42, 0.06);
    position: relative;
}}

.profile-hero .hero-row {{
    display: grid;
    grid-template-columns: 1fr auto;
    gap: 1rem;
    align-items: center;
}}

.profile-hero .hero-chip {{
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.4rem 0.8rem;
    background: #fff;
    border: 1px dashed #cfd8ff;
    border-radius: 12px;
    font-weight: 700;
    color: #1e3a8a;
    font-size: 12px;
}}

.profile-hero h2 {{
    margin: 0.35rem 0;
    color: #0f172a;
    font-weight: 800;
}}

.profile-hero .hero-meta {{
    color: #475569;
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
}}

.profile-hero .avatar-circle-large {{
    width: 96px;
    height: 96px;
    border-radius: 22px;
    background: linear-gradient(145deg, #1a73e8 0%, #0d47a1 100%);
    color: white;
    display: grid;
    place-items: center;
    font-weight: 800;
    font-size: 36px;
    box-shadow: 0 12px 30px rgba(13, 71, 161, 0.28);
}}

.profile-hero .contact-detail-item i {{
    color: #1a73e8;
}}

.profile-actions {{
    display: flex;
    gap: 0.75rem;
    flex-wrap: wrap;
    margin-top: 1rem;
}}

.profile-actions .btn-edit-profile {{
    background: linear-gradient(135deg, #1a73e8 0%, #0d47a1 100%);
    color: white;
    border: none;
    padding: 0.65rem 1.25rem;
    border-radius: 10px;
    box-shadow: 0 10px 20px rgba(13, 71, 161, 0.26);
}}

.profile-actions .btn-account-details {{
    background: #fff;
    border: 1px solid #e5e7eb;
    padding: 0.65rem 1.25rem;
    border-radius: 10px;
    color: #111827;
}}

.info-pills {{
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 0.75rem;
    margin-top: 1.25rem;
}}

.info-pill {{
    background: #fff;
    border: 1px solid #e5e7eb;
    border-radius: 12px;
    padding: 1rem 1.1rem;
    box-shadow: 0 6px 16px rgba(15, 23, 42, 0.06);
}}

.info-pill .label {{
    color: #94a3b8;
    font-size: 12px;
}}

.info-pill .value {{
    font-size: 18px;
    font-weight: 800;
    color: #0f172a;
}}

.info-pill .desc {{
    color: #9ca3af;
    font-size: 12px;
}}

.info-cards-row {{
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    gap: 1rem;
    margin-top: 1.25rem;
}}

.info-card {{
    background: white;
    padding: 1.25rem;
    border-radius: 14px;
    border: 1px solid #eef2f7;
    transition: all 0.3s;
    box-shadow: 0 8px 24px rgba(15, 23, 42, 0.06);
}}

.info-card:hover {{
    box-shadow: 0 12px 32px rgba(15, 23, 42, 0.08);
    transform: translateY(-2px);
}}

.info-card-header {{
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 1rem;
}}

.info-card-header i {{
    color: #1a73e8;
    font-size: 20px;
}}

.info-card-header h6 {{
    margin: 0;
    color: #0f172a;
    font-weight: 700;
    font-size: 14px;
}}

.info-value {{
    font-size: 22px;
    font-weight: 800;
    color: #0f172a;
    margin-bottom: 0.35rem;
}}

.info-description {{
    font-size: 12px;
    color: #94a3b8;
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

/* Dashboard Widgets */
.section-title-bar {{
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 1rem;
}}

.dashboard-hero {{
    background: linear-gradient(135deg, #f2f6ff 0%, #dbe8ff 100%);
    border-radius: 18px;
    padding: 1.5rem;
    border: 1px solid #e3ebff;
    box-shadow: 0 10px 30px rgba(25, 118, 210, 0.08);
    display: grid;
    grid-template-columns: 1fr auto;
    gap: 1rem;
    align-items: center;
}}

.dashboard-hero .hero-label {{
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.35rem 0.75rem;
    border-radius: 999px;
    background: rgba(33, 150, 243, 0.12);
    color: #1565c0;
    font-weight: 700;
    font-size: 12px;
    letter-spacing: 0.2px;
}}

.dashboard-hero h3 {{
    margin: 0.5rem 0;
    font-weight: 800;
    color: #0f172a;
}}

.dashboard-hero p {{
    color: #4b5563;
    margin: 0;
    line-height: 1.7;
}}

.hero-meta {{
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
    margin-top: 0.75rem;
}}

.meta-chip {{
    padding: 0.35rem 0.75rem;
    background: #fff;
    border-radius: 10px;
    border: 1px dashed #cbd5e1;
    color: #0f172a;
    font-size: 12px;
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
}}

.stat-stack {{
    display: flex;
    align-items: center;
    gap: 0.75rem;
}}

.stat-circle {{
    width: 78px;
    height: 78px;
    border-radius: 22px;
    background: linear-gradient(145deg, #1a73e8 0%, #0d47a1 100%);
    color: #fff;
    display: grid;
    place-items: center;
    font-weight: 800;
    font-size: 26px;
    box-shadow: 0 12px 30px rgba(13, 71, 161, 0.3);
}}

.stat-note {{
    font-size: 13px;
    color: #1f2937;
    display: grid;
    gap: 0.15rem;
}}

.dashboard-grid {{
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(230px, 1fr));
    gap: 1rem;
    margin-bottom: 1.25rem;
}}

.summary-card {{
    background: #fff;
    border-radius: 14px;
    padding: 1.2rem;
    border: 1px solid #eef2f7;
    box-shadow: 0 10px 30px rgba(15, 23, 42, 0.06);
    transition: transform 0.25s ease, box-shadow 0.25s ease;
}}

.summary-card:hover {{
    transform: translateY(-3px);
    box-shadow: 0 14px 40px rgba(15, 23, 42, 0.08);
}}

.summary-card .icon-badge {{
    width: 48px;
    height: 48px;
    border-radius: 12px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    color: white;
    margin-bottom: 0.5rem;
}}

.summary-card .label {{
    color: #6b7280;
    font-size: 13px;
    font-weight: 600;
}}

.summary-card .value {{
    font-size: 28px;
    font-weight: 800;
    color: #0f172a;
    margin: 0.15rem 0;
}}

.summary-card .desc {{
    color: #9ca3af;
    font-size: 12px;
    margin: 0;
}}

.summary-card.primary {{
    border-color: rgba(102, 126, 234, 0.35);
}}

.summary-card.primary .icon-badge {{
    background: linear-gradient(135deg, #009ef7 0%, #1a5ed8 100%);
    box-shadow: 0 8px 20px rgba(0, 158, 247, 0.3);
}}

.summary-card.success {{
    border-color: rgba(39, 174, 96, 0.35);
}}

.summary-card.success .icon-badge {{
    background: linear-gradient(135deg, #0bb783 0%, #1bc5bd 100%);
    box-shadow: 0 8px 20px rgba(27, 197, 189, 0.26);
}}

.summary-card.warning {{
    border-color: rgba(255, 193, 7, 0.35);
}}

.summary-card.warning .icon-badge {{
    background: linear-gradient(135deg, #f1c40f 0%, #f39c12 100%);
    box-shadow: 0 8px 20px rgba(243, 156, 18, 0.25);
}}

.summary-card.info {{
    border-color: rgba(52, 152, 219, 0.35);
}}

.summary-card.info .icon-badge {{
    background: linear-gradient(135deg, #50cd89 0%, #1da1f2 100%);
    box-shadow: 0 8px 20px rgba(80, 205, 137, 0.26);
}}

.action-card {{
    background: white;
    border-radius: 14px;
    padding: 1.25rem;
    border: 1px solid #eef2f7;
    box-shadow: 0 8px 24px rgba(15, 23, 42, 0.06);
}}

.action-card .list-group {{
    margin-bottom: 0;
}}

.action-card .list-group-item {{
    border: none;
    padding: 0.8rem 0;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
    font-weight: 600;
    color: #111827;
}}

.action-card .list-group-item + .list-group-item {{
    border-top: 1px solid #f1f5f9;
}}

.action-card .list-group-item i {{
    color: var(--primary-color);
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
            "Views/Orders",
            "Views/Products",
            "Views/Wallet",
            "Views/Invoice"
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

        File.WriteAllText(
            Path.Combine(userPath, "Controllers", "ProductsController.cs"),
            _templates.GetUserProductsControllerTemplate()
        );

        File.WriteAllText(
            Path.Combine(userPath, "Controllers", "WalletController.cs"),
            _templates.GetUserWalletControllerTemplate()
        );

        File.WriteAllText(
            Path.Combine(userPath, "Controllers", "InvoiceController.cs"),
            _templates.GetUserInvoicesControllerTemplate()
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

        // User products / wallet / invoice views from reference
        var viewReplacements = new Dictionary<string, string>
        {
            ["EndPoint.WebSite"] = $"{_config.ProjectName}.WebSite"
        };

        TryCopyReferenceFile(
            Path.Combine("Areas", "User", "Views", "Products", "Index.cshtml"),
            Path.Combine(userPath, "Views", "Products", "Index.cshtml"),
            viewReplacements);

        TryCopyReferenceFile(
            Path.Combine("Areas", "User", "Views", "Wallet", "Index.cshtml"),
            Path.Combine(userPath, "Views", "Wallet", "Index.cshtml"),
            viewReplacements);

        TryCopyReferenceFile(
            Path.Combine("Areas", "User", "Views", "Wallet", "InvoiceDetails.cshtml"),
            Path.Combine(userPath, "Views", "Wallet", "InvoiceDetails.cshtml"),
            viewReplacements);

        TryCopyReferenceFile(
            Path.Combine("Areas", "User", "Views", "Wallet", "PayInvoice.cshtml"),
            Path.Combine(userPath, "Views", "Wallet", "PayInvoice.cshtml"),
            viewReplacements);

        TryCopyReferenceFile(
            Path.Combine("Areas", "User", "Views", "Wallet", "BankPaymentSession.cshtml"),
            Path.Combine(userPath, "Views", "Wallet", "BankPaymentSession.cshtml"),
            viewReplacements);

        var invoiceIndexReplacements = new Dictionary<string, string>(viewReplacements)
        {
            ["@model Arsis.Application.DTOs.Billing.InvoiceListResultDto"] = $"@model {_config.ProjectName}.WebSite.Areas.User.Models.UserInvoiceListViewModel"
        };

        TryCopyReferenceFile(
            Path.Combine("Areas", "User", "Views", "Invoice", "Index.cshtml"),
            Path.Combine(userPath, "Views", "Invoice", "Index.cshtml"),
            invoiceIndexReplacements);

        var invoiceDetailReplacements = new Dictionary<string, string>(viewReplacements)
        {
            ["@model Arsis.Application.DTOs.Billing.InvoiceDetailDto"] = $"@model {_config.ProjectName}.WebSite.Areas.User.Models.UserInvoiceDetailViewModel",
            ["@using Arsis.Domain.Enums"] = string.Empty
        };

        TryCopyReferenceFile(
            Path.Combine("Areas", "User", "Views", "Invoice", "Details.cshtml"),
            Path.Combine(userPath, "Views", "Invoice", "Details.cshtml"),
            invoiceDetailReplacements);
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

        // Main public layout is generated from templates
        File.WriteAllText(
            Path.Combine(sharedPath, "_Layout.cshtml"),
            _templates.GetLayoutTemplate(_config.Theme)
        );

        // Admin/User/Seller panel layouts are copied from pre-built Razor templates
        // located in the generator's wwwroot folder. This keeps the panel UIs
        // pixel-perfect copies of the reference implementation (ArsisTest) without
        // embedding huge Razor strings inside C# source.
        CopyPanelLayoutFromTemplatesRoot("_AdminLayout.cshtml", Path.Combine(sharedPath, "_AdminLayout.cshtml"));

        if (_config.Options.Features.SellerPanel)
        {
            CopyPanelLayoutFromTemplatesRoot("_SellerLayout.cshtml", Path.Combine(sharedPath, "_SellerLayout.cshtml"));
        }

        CopyPanelLayoutFromTemplatesRoot("_UserLayout.cshtml", Path.Combine(sharedPath, "_UserLayout.cshtml"));

        // Shared alert modal partial used by all panel layouts
        CopyPanelLayoutFromTemplatesRoot("_AlertModal.cshtml", Path.Combine(sharedPath, "_AlertModal.cshtml"));

        File.WriteAllText(
            Path.Combine(sharedPath, "_StatusMessage.cshtml"),
            _templates.GetStatusMessagePartialTemplate()
        );

        File.WriteAllText(
            Path.Combine(sharedPath, "_ValidationScriptsPartial.cshtml"),
            _templates.GetValidationScriptsPartialTemplate()
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

        // Generate shared extensions/helpers
        GenerateExtensions();
    }

    /// <summary>
    /// Copies a Razor layout/partial file from the generator's template wwwroot
    /// folder into the generated WebSite project, if the source file exists.
    /// This method tries to be robust to different entry assemblies (console UI,
    /// WinForms UI, tests) by walking up the directory tree and also checking
    /// for a sibling ProjectGenerator/wwwroot folder.
    /// </summary>
    private void CopyPanelLayoutFromTemplatesRoot(string fileName, string destinationPath)
    {
        try
        {
            var sourcePath = FindPanelTemplatePath(fileName);

            if (sourcePath == null)
            {
                Console.WriteLine($"⚠ Warning: Panel layout template '{fileName}' could not be located in generator wwwroot.");
                return;
            }

            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Warning: Failed to copy panel layout '{fileName}': {ex.Message}");
        }
    }

    private static string? FindPanelTemplatePath(string fileName)
    {
        // Start from the entry assembly base directory
        var current = AppDomain.CurrentDomain.BaseDirectory;
        
        // 1) Walk up a few levels and look for a local wwwroot
        for (var i = 0; i < 6 && !string.IsNullOrEmpty(current); i++)
        {
            var direct = Path.Combine(current, "wwwroot", fileName);
            if (File.Exists(direct))
            {
                return direct;
            }
            
            // Also check for a sibling ProjectGenerator/wwwroot folder
            var siblingProjectGen = Path.Combine(current, "ProjectGenerator", "wwwroot", fileName);
            if (File.Exists(siblingProjectGen))
            {
                return siblingProjectGen;
            }
            
            var parent = Directory.GetParent(current);
            if (parent == null)
            {
                break;
            }
            current = parent.FullName;
        }
        
        return null;
    }

    private static string? FindComponentsTemplateRoot()
    {
        // Start from the entry assembly base directory and walk up,
        // looking for a wwwroot/Components folder belonging to the generator.
        var current = AppDomain.CurrentDomain.BaseDirectory;

        for (var i = 0; i < 6 && !string.IsNullOrEmpty(current); i++)
        {
            var direct = Path.Combine(current, "wwwroot", "Components");
            if (Directory.Exists(direct))
            {
                return direct;
            }

            // Also check for a sibling ProjectGenerator/wwwroot/Components folder
            var siblingProjectGen = Path.Combine(current, "ProjectGenerator", "wwwroot", "Components");
            if (Directory.Exists(siblingProjectGen))
            {
                return siblingProjectGen;
            }

            var parent = Directory.GetParent(current);
            if (parent == null)
            {
                break;
            }
            current = parent.FullName;
        }

        return null;
    }

    private static string? FindGeneratorWwwroot()
    {
        // Start from the entry assembly base directory and walk up,
        // looking for the generator's wwwroot folder that contains
        // static assets (css, js, lib, font, etc.).
        var current = AppDomain.CurrentDomain.BaseDirectory;

        for (var i = 0; i < 6 && !string.IsNullOrEmpty(current); i++)
        {
            var direct = Path.Combine(current, "wwwroot");
            var isDevOutput = current.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                              current.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}");
            if (Directory.Exists(direct) && !isDevOutput)
            {
                return direct;
            }

            // Also check for a sibling ProjectGenerator/wwwroot folder
            var siblingProjectGen = Path.Combine(current, "ProjectGenerator", "wwwroot");
            if (Directory.Exists(siblingProjectGen))
            {
                return siblingProjectGen;
            }

            var parent = Directory.GetParent(current);
            if (parent == null)
            {
                break;
            }
            current = parent.FullName;
        }

        return null;
    }

    private bool TryCopyReferenceFile(string relativePath, string destinationPath, IDictionary<string, string>? replacements = null)
    {
        var referenceRoot = ReferencePaths.FindReferenceProjectRoot();
        if (referenceRoot is null)
        {
            Console.WriteLine($"⚠ Warning: Reference project root could not be located. Skipping copy of '{relativePath}'.");
            return false;
        }

        var sourcePath = Path.Combine(referenceRoot, relativePath);
        if (!File.Exists(sourcePath))
        {
            Console.WriteLine($"⚠ Warning: Reference file '{relativePath}' does not exist.");
            return false;
        }

        var content = File.ReadAllText(sourcePath);
        if (replacements is not null)
        {
            foreach (var pair in replacements)
            {
                content = content.Replace(pair.Key, pair.Value);
            }
        }

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(destinationPath, content);
        return true;
    }

    private void CopyCssFromTemplate()
    {
        var templateRoot = FindGeneratorWwwroot();
        if (templateRoot == null)
        {
            return;
        }

        var sourceCssDir = Path.Combine(templateRoot, "css");
        var targetCssDir = Path.Combine(_websitePath, "wwwroot", "css");

        if (Directory.Exists(sourceCssDir))
        {
            CopyDirectory(sourceCssDir, targetCssDir, recursive: true);
        }
    }

    private void FixComponentViewModelNamespaces(string componentsRoot)
    {
        // The WebSite project name is "<ProjectName>.WebSite"
        var rootNamespace = $"{_config.ProjectName}.WebSite";

        void PatchModel(string relativePath, string simpleModelName, string qualifiedModelName)
        {
            var fullPath = Path.Combine(componentsRoot, relativePath);
            if (!File.Exists(fullPath))
            {
                return;
            }

            var content = File.ReadAllText(fullPath);

            // If it's already using the qualified name, skip.
            if (content.Contains($"@model {qualifiedModelName}"))
            {
                return;
            }

            if (content.Contains($"@model {simpleModelName}"))
            {
                content = content.Replace($"@model {simpleModelName}", $"@model {qualifiedModelName}");
                File.WriteAllText(fullPath, content);
            }
        }

        PatchModel(
            Path.Combine("AdminSidebar", "Default.cshtml"),
            "AdminSidebarViewModel",
            $"{rootNamespace}.ViewComponents.AdminSidebarViewModel");

        PatchModel(
            Path.Combine("UserSidebar", "Default.cshtml"),
            "UserSidebarViewModel",
            $"{rootNamespace}.ViewComponents.UserSidebarViewModel");

        // Teacher sidebar reuses the same SellerSidebarViewModel type
        PatchModel(
            Path.Combine("SellerSidebar", "Default.cshtml"),
            "SellerSidebarViewModel",
            $"{rootNamespace}.ViewComponents.SellerSidebarViewModel");

        PatchModel(
            Path.Combine("TeacherSidebar", "Default.cshtml"),
            "SellerSidebarViewModel",
            $"{rootNamespace}.ViewComponents.SellerSidebarViewModel");

        PatchModel(
            Path.Combine("CartPreview", "Default.cshtml"),
            "CartPreviewViewModel",
            $"{rootNamespace}.Models.Cart.CartPreviewViewModel");
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

        File.WriteAllText(
            Path.Combine(userModelsPath, "UserSettingsViewModel.cs"),
            _templates.GetUserSettingsViewModelTemplate()
        );

        File.WriteAllText(
            Path.Combine(userModelsPath, "UserProductViewModels.cs"),
            _templates.GetUserProductViewModelsTemplate()
        );

        File.WriteAllText(
            Path.Combine(userModelsPath, "WalletViewModels.cs"),
            _templates.GetWalletViewModelsTemplate()
        );

        File.WriteAllText(
            Path.Combine(userModelsPath, "UserInvoiceViewModels.cs"),
            _templates.GetUserInvoiceListViewModelTemplate()
        );

        // Cart / shared ViewModels
        var cartModelsPath = Path.Combine(modelsPath, "Cart");
        Directory.CreateDirectory(cartModelsPath);
        File.WriteAllText(
            Path.Combine(cartModelsPath, "CartPreviewViewModel.cs"),
            _templates.GetCartPreviewViewModelTemplate()
        );
    }

    private void GenerateLaunchSettings()
    {
        var propertiesPath = Path.Combine(_websitePath, "Properties");
        Directory.CreateDirectory(propertiesPath);
        
        var launchSettings = @"{
  ""$schema"": ""http://json.schemastore.org/launchsettings.json"",
  ""iisSettings"": {
    ""windowsAuthentication"": false,
    ""anonymousAuthentication"": true,
    ""iisExpress"": {
      ""applicationUrl"": ""http://localhost:5000"",
      ""sslPort"": 44300
    }
  },
  ""profiles"": {
    """ + _config.ProjectName + @".WebSite"": {
      ""commandName"": ""Project"",
      ""dotnetRunMessages"": true,
      ""launchBrowser"": true,
      ""applicationUrl"": ""https://localhost:7000;http://localhost:5000"",
      ""environmentVariables"": {
        ""ASPNETCORE_ENVIRONMENT"": ""Development""
      }
    },
    ""IIS Express"": {
      ""commandName"": ""IISExpress"",
      ""launchBrowser"": true,
      ""environmentVariables"": {
        ""ASPNETCORE_ENVIRONMENT"": ""Development""
      }
    }
  }
}";
        
        File.WriteAllText(Path.Combine(propertiesPath, "launchSettings.json"), launchSettings);
        Console.WriteLine("✓ launchSettings.json created");
    }

    private void GenerateExtensions()
    {
        var extensionsPath = Path.Combine(_websitePath, "Extensions");
        Directory.CreateDirectory(extensionsPath);

        File.WriteAllText(
            Path.Combine(extensionsPath, "PersianDateExtensions.cs"),
            _templates.GetPersianDateExtensionsTemplate()
        );
    }

    private void CopyWwwrootFiles()
    {
        try
        {
            // Resolve the generator's wwwroot folder in a way that works
            // for console, WinForms UI, and test hosts.
            var sourceWwwroot = FindGeneratorWwwroot();
            var targetWwwroot = Path.Combine(_websitePath, "wwwroot");

            if (sourceWwwroot is null || !Directory.Exists(sourceWwwroot))
            {
                Console.WriteLine("⚠ Warning: Source wwwroot could not be located for static assets (bootstrap, jquery, fonts).");
                return;
            }

            Console.WriteLine($"Copying wwwroot files from template: {sourceWwwroot} ...");
            CopyDirectory(sourceWwwroot, targetWwwroot, true);
            var layoutCandidates = new[]
            {
                "_AdminLayout.cshtml",
                "_SellerLayout.cshtml",
                "_UserLayout.cshtml",
                "_TeacherLayout.cshtml",
                "_TeacherLayout.raw.cshtml",
                "_AlertModal.cshtml"
            };

            foreach (var layoutFile in layoutCandidates)
            {
                var strayPath = Path.Combine(targetWwwroot, layoutFile);
                if (File.Exists(strayPath))
                {
                    try
                    {
                        File.Delete(strayPath);
                    }
                    catch
                    {
                        Console.WriteLine($"⚠ Warning: Could not remove stray layout file {strayPath} from wwwroot.");
                    }
                }
            }

            Console.WriteLine("✓ Wwwroot files copied successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Warning: Failed to copy wwwroot files: {ex.Message}");
        }
    }
    
    private void CopyDirectory(string sourceDir, string targetDir, bool recursive)
    {
        var dir = new DirectoryInfo(sourceDir);
        
        if (!dir.Exists)
        {
            return;
        }
        
        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(targetDir);
        
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(targetDir, file.Name);
            try
            {
                file.CopyTo(targetFilePath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Warning: Failed to copy {file.Name}: {ex.Message}");
            }
        }
        
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(targetDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
    
    private void GenerateViewComponents()
    {
        var viewComponentsPath = Path.Combine(_websitePath, "ViewComponents");
        Directory.CreateDirectory(viewComponentsPath);
        
        // Generate Admin Sidebar ViewComponent
        File.WriteAllText(
            Path.Combine(viewComponentsPath, "AdminSidebarViewComponent.cs"),
            _templates.GetAdminSidebarViewComponentTemplate()
        );
        
        // Generate Seller Sidebar ViewComponent (if enabled)
        if (_config.Options.Features.SellerPanel)
        {
            File.WriteAllText(
                Path.Combine(viewComponentsPath, "SellerSidebarViewComponent.cs"),
                _templates.GetSellerSidebarViewComponentTemplate()
            );
        }
        
        // Generate User Sidebar ViewComponent
        File.WriteAllText(
            Path.Combine(viewComponentsPath, "UserSidebarViewComponent.cs"),
            _templates.GetUserSidebarViewComponentTemplate()
        );
        
        // Copy ViewComponent views into Views/Shared/Components
        var targetComponentsPath = Path.Combine(_websitePath, "Views", "Shared", "Components");
        Directory.CreateDirectory(targetComponentsPath);

        // 1) Preferred: copy directly from generator's template Components folder
        var templateComponentsRoot = FindComponentsTemplateRoot();
        if (templateComponentsRoot is not null && Directory.Exists(templateComponentsRoot))
        {
            CopyDirectory(templateComponentsRoot, targetComponentsPath, recursive: true);
        }
        else
        {
            // 2) Fallback: copy from generated wwwroot/Components if it exists
            var sourceComponentsPath = Path.Combine(_websitePath, "wwwroot", "Components");
            if (Directory.Exists(sourceComponentsPath))
            {
                CopyDirectory(sourceComponentsPath, targetComponentsPath, recursive: true);

                // Clean up the wwwroot/Components directory after copying
                try
                {
                    Directory.Delete(sourceComponentsPath, true);
                }
                catch
                {
                    // Ignore if we can't delete
                }
            }
            else
            {
                Console.WriteLine("⚠ Warning: Could not locate sidebar component views in either template or generated wwwroot.");
            }
        }

        // Ensure component views use fully-qualified model types so they
        // compile without relying on custom _ViewImports changes.
        FixComponentViewModelNamespaces(targetComponentsPath);
        
        var oldComponentsDir = Path.Combine(_websitePath, "wwwroot", "Components");
        if (Directory.Exists(oldComponentsDir))
        {
            try
            {
                Directory.Delete(oldComponentsDir, true);
            }
            catch
            {
                Console.WriteLine($"⚠ Warning: Could not delete temporary Components directory at {oldComponentsDir}.");
            }
        }

        Console.WriteLine("✓ ViewComponents generated successfully");
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
