using ProjectGenerator.Models;
using ProjectGenerator.Templates;

namespace ProjectGenerator.Generators;

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
            _templates.GetLayoutTemplate()
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
    }
}
