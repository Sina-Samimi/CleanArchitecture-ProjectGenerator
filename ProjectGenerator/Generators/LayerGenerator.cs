using ProjectGenerator.Models;
using ProjectGenerator.Templates;

namespace ProjectGenerator.Generators;

public enum LayerType
{
    Domain,
    SharedKernel,
    Application,
    Infrastructure,
    Tests,
    WebSite
}

public class LayerGenerator
{
    private readonly ProjectConfig _config;
    private readonly TemplateProvider _templateProvider;

    public LayerGenerator(ProjectConfig config)
    {
        _config = config;
        _templateProvider = new TemplateProvider(config.Namespace);
    }

    public void GenerateLayer(string layerName, LayerType layerType)
    {
        var layerPath = layerType == LayerType.Tests
            ? Path.Combine(_config.OutputPath, "tests", layerName)
            : Path.Combine(_config.OutputPath, "src", layerName);

        Directory.CreateDirectory(layerPath);
        Console.WriteLine($"Creating {layerName} layer...");

        switch (layerType)
        {
            case LayerType.Domain:
                GenerateDomainLayer(layerPath, layerName);
                break;
            case LayerType.SharedKernel:
                GenerateSharedKernelLayer(layerPath, layerName);
                break;
            case LayerType.Application:
                GenerateApplicationLayer(layerPath, layerName);
                break;
            case LayerType.Infrastructure:
                GenerateInfrastructureLayer(layerPath, layerName);
                break;
            case LayerType.Tests:
                GenerateTestsLayer(layerPath, layerName);
                break;
        }

        Console.WriteLine($"✓ {layerName} layer created");
    }

    public void GenerateWebSiteLayer()
    {
        var layerPath = Path.Combine(_config.OutputPath, "src", $"{_config.ProjectName}.WebSite");
        Directory.CreateDirectory(layerPath);
        Console.WriteLine($"Creating WebSite layer...");

        // Create project file
        var csprojContent = _templateProvider.GetWebSiteCsprojTemplate(_config.ProjectName);
        File.WriteAllText(Path.Combine(layerPath, $"{_config.ProjectName}.WebSite.csproj"), csprojContent);

        // Create Program.cs with enhanced configuration
        var programContent = _templateProvider.GetEnhancedProgramTemplate();
        File.WriteAllText(Path.Combine(layerPath, "Program.cs"), programContent);

        // Create appsettings
        var appSettingsContent = _templateProvider.GetAppSettingsTemplate();
        File.WriteAllText(Path.Combine(layerPath, "appsettings.json"), appSettingsContent);

        // Use WebSiteGenerator for complete generation
        var websiteGenerator = new WebSiteGenerator(_config, layerPath);
        websiteGenerator.Generate();

        Console.WriteLine($"✓ WebSite layer created");
    }

    private void GenerateDomainLayer(string layerPath, string layerName)
    {
        // Create csproj
        var csprojContent = _templateProvider.GetBasicCsprojTemplate(layerName);
        File.WriteAllText(Path.Combine(layerPath, $"{layerName}.csproj"), csprojContent);

        // Create directories
        var dirs = new[] { "Entities", "Enums", "Base" };
        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(layerPath, dir));
        }

        // Create base classes
        var basePath = Path.Combine(layerPath, "Base");
        File.WriteAllText(
            Path.Combine(basePath, "Entity.cs"),
            _templateProvider.GetBaseEntityTemplate()
        );
        
        File.WriteAllText(
            Path.Combine(basePath, "SeoEntity.cs"),
            _templateProvider.GetSeoEntityTemplate()
        );

        // Generate all core domain entities
        var entitiesPath = Path.Combine(layerPath, "Entities");

        // Core Identity
        File.WriteAllText(
            Path.Combine(entitiesPath, "ApplicationUser.cs"),
            _templateProvider.GetEnhancedApplicationUserTemplate()
        );

        // Product & Catalog
        File.WriteAllText(
            Path.Combine(entitiesPath, "Product.cs"),
            _templateProvider.GetProductEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "ProductImage.cs"),
            _templateProvider.GetProductImageEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "ProductComment.cs"),
            _templateProvider.GetProductCommentEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "ProductExecutionStep.cs"),
            _templateProvider.GetProductExecutionStepEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "ProductFaq.cs"),
            _templateProvider.GetProductFaqEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "SiteCategory.cs"),
            _templateProvider.GetSiteCategoryEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "DiscountCode.cs"),
            _templateProvider.GetDiscountCodeEntityTemplate()
        );

        // Blog
        File.WriteAllText(
            Path.Combine(entitiesPath, "Blog.cs"),
            _templateProvider.GetBlogEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "BlogComment.cs"),
            _templateProvider.GetBlogCommentEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "BlogCategory.cs"),
            _templateProvider.GetBlogCategoryEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "BlogAuthor.cs"),
            _templateProvider.GetBlogAuthorEntityTemplate()
        );

        // Billing & Wallet
        File.WriteAllText(
            Path.Combine(entitiesPath, "Invoice.cs"),
            _templateProvider.GetInvoiceEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "InvoiceItem.cs"),
            _templateProvider.GetInvoiceItemEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "Transaction.cs"),
            _templateProvider.GetTransactionEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "WalletAccount.cs"),
            _templateProvider.GetWalletAccountEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "WalletTransaction.cs"),
            _templateProvider.GetWalletTransactionEntityTemplate()
        );

        // Settings
        File.WriteAllText(
            Path.Combine(entitiesPath, "SiteSetting.cs"),
            _templateProvider.GetSiteSettingEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "NavigationMenuItem.cs"),
            _templateProvider.GetNavigationMenuItemEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "FinancialSettings.cs"),
            _templateProvider.GetFinancialSettingsEntityTemplate()
        );

        // Sellers
        File.WriteAllText(
            Path.Combine(entitiesPath, "SellerProfile.cs"),
            _templateProvider.GetSellerProfileEntityTemplate()
        );

        // Permissions & Access
        File.WriteAllText(
            Path.Combine(entitiesPath, "AccessPermission.cs"),
            _templateProvider.GetAccessPermissionEntityTemplate()
        );
        File.WriteAllText(
            Path.Combine(entitiesPath, "PageAccessPolicy.cs"),
            _templateProvider.GetPageAccessPolicyEntityTemplate()
        );

        // Sessions
        File.WriteAllText(
            Path.Combine(entitiesPath, "UserSession.cs"),
            _templateProvider.GetUserSessionEntityTemplate()
        );

        // Generate all Enums
        var enumsPath = Path.Combine(layerPath, "Enums");
        File.WriteAllText(
            Path.Combine(enumsPath, "ProductType.cs"),
            _templateProvider.GetProductTypeEnumTemplate()
        );
        File.WriteAllText(
            Path.Combine(enumsPath, "BlogStatus.cs"),
            _templateProvider.GetBlogStatusEnumTemplate()
        );
        File.WriteAllText(
            Path.Combine(enumsPath, "CategoryScope.cs"),
            _templateProvider.GetCategoryScopeEnumTemplate()
        );
        File.WriteAllText(
            Path.Combine(enumsPath, "InvoiceStatus.cs"),
            _templateProvider.GetInvoiceStatusEnumTemplate()
        );
        File.WriteAllText(
            Path.Combine(enumsPath, "DiscountType.cs"),
            _templateProvider.GetDiscountTypeEnumTemplate()
        );
        File.WriteAllText(
            Path.Combine(enumsPath, "Currency.cs"),
            _templateProvider.GetCurrencyEnumTemplate()
        );
    }

    private void GenerateSharedKernelLayer(string layerPath, string layerName)
    {
        var csprojContent = _templateProvider.GetBasicCsprojTemplate(layerName);
        File.WriteAllText(Path.Combine(layerPath, $"{layerName}.csproj"), csprojContent);

        var dirs = new[] { "Interfaces", "Guards", "Results" };
        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(layerPath, dir));
        }

        // Create IRepository
        var interfacesPath = Path.Combine(layerPath, "Interfaces");
        File.WriteAllText(
            Path.Combine(interfacesPath, "IRepository.cs"),
            _templateProvider.GetIRepositoryTemplate()
        );

        // Create Result pattern
        var resultsPath = Path.Combine(layerPath, "Results");
        File.WriteAllText(
            Path.Combine(resultsPath, "Result.cs"),
            _templateProvider.GetResultTemplate()
        );
    }

    private void GenerateApplicationLayer(string layerPath, string layerName)
    {
        var csprojContent = _templateProvider.GetApplicationCsprojTemplate(layerName);
        File.WriteAllText(Path.Combine(layerPath, $"{layerName}.csproj"), csprojContent);

        var dirs = new[] { "Interfaces", "Services", "DTOs", "Mapping" };
        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(layerPath, dir));
        }

        var features = _config.Options.Features;
        var interfacesPath = Path.Combine(layerPath, "Interfaces");
        var dtosPath = Path.Combine(layerPath, "DTOs");

        // Generate services and DTOs based on features
        if (features.ProductCatalog)
        {
            // Create DTO directories
            Directory.CreateDirectory(Path.Combine(dtosPath, "Product"));
            Directory.CreateDirectory(Path.Combine(dtosPath, "Category"));

            // Generate interfaces
            File.WriteAllText(
                Path.Combine(interfacesPath, "IProductService.cs"),
                _templateProvider.GetIProductServiceTemplate()
            );
            File.WriteAllText(
                Path.Combine(interfacesPath, "ICategoryService.cs"),
                _templateProvider.GetICategoryServiceTemplate()
            );

            // Generate DTOs
            File.WriteAllText(
                Path.Combine(dtosPath, "Product", "ProductDtos.cs"),
                _templateProvider.GetProductDtosTemplate()
            );
            File.WriteAllText(
                Path.Combine(dtosPath, "Category", "CategoryDtos.cs"),
                _templateProvider.GetCategoryDtosTemplate()
            );
        }

        if (features.ShoppingCart)
        {
            Directory.CreateDirectory(Path.Combine(dtosPath, "Cart"));
            
            File.WriteAllText(
                Path.Combine(interfacesPath, "ICartService.cs"),
                _templateProvider.GetICartServiceTemplate()
            );
            File.WriteAllText(
                Path.Combine(dtosPath, "Cart", "CartDtos.cs"),
                _templateProvider.GetCartDtosTemplate()
            );
        }

        if (features.Invoicing)
        {
            Directory.CreateDirectory(Path.Combine(dtosPath, "Order"));
            Directory.CreateDirectory(Path.Combine(dtosPath, "Invoice"));
            
            File.WriteAllText(
                Path.Combine(interfacesPath, "IOrderService.cs"),
                _templateProvider.GetIOrderServiceTemplate()
            );
            File.WriteAllText(
                Path.Combine(interfacesPath, "IInvoiceService.cs"),
                _templateProvider.GetIInvoiceServiceTemplate()
            );
            File.WriteAllText(
                Path.Combine(dtosPath, "Order", "OrderDtos.cs"),
                _templateProvider.GetOrderDtosTemplate()
            );
            File.WriteAllText(
                Path.Combine(dtosPath, "Invoice", "InvoiceDtos.cs"),
                _templateProvider.GetInvoiceDtosTemplate()
            );
        }

        if (features.BlogSystem)
        {
            Directory.CreateDirectory(Path.Combine(dtosPath, "Blog"));
            
            File.WriteAllText(
                Path.Combine(interfacesPath, "IBlogService.cs"),
                _templateProvider.GetIBlogServiceTemplate()
            );
            File.WriteAllText(
                Path.Combine(dtosPath, "Blog", "BlogDtos.cs"),
                _templateProvider.GetBlogDtosTemplate()
            );
        }

        // Create DependencyInjection
        File.WriteAllText(
            Path.Combine(layerPath, "DependencyInjection.cs"),
            _templateProvider.GetApplicationDependencyInjectionTemplate()
        );
    }

    private void GenerateInfrastructureLayer(string layerPath, string layerName)
    {
        var csprojContent = _templateProvider.GetInfrastructureCsprojTemplate();
        File.WriteAllText(Path.Combine(layerPath, $"{layerName}.csproj"), csprojContent);

        var dirs = new[] { "Persistence", "Persistence/Configurations", "Services" };
        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(layerPath, dir));
        }

        // Create DbContext
        var persistencePath = Path.Combine(layerPath, "Persistence");
        File.WriteAllText(
            Path.Combine(persistencePath, "ApplicationDbContext.cs"),
            _templateProvider.GetApplicationDbContextTemplate()
        );

        // Create Entity Configurations
        var configPath = Path.Combine(persistencePath, "Configurations");
        File.WriteAllText(
            Path.Combine(configPath, "ProductConfiguration.cs"),
            _templateProvider.GetProductEntityConfigurationTemplate()
        );
        File.WriteAllText(
            Path.Combine(configPath, "BlogConfiguration.cs"),
            _templateProvider.GetBlogEntityConfigurationTemplate()
        );
        File.WriteAllText(
            Path.Combine(configPath, "InvoiceConfiguration.cs"),
            _templateProvider.GetInvoiceEntityConfigurationTemplate()
        );
        File.WriteAllText(
            Path.Combine(configPath, "SiteCategoryConfiguration.cs"),
            _templateProvider.GetSiteCategoryEntityConfigurationTemplate()
        );
        File.WriteAllText(
            Path.Combine(configPath, "AccessPermissionConfiguration.cs"),
            _templateProvider.GetAccessPermissionEntityConfigurationTemplate()
        );
        File.WriteAllText(
            Path.Combine(configPath, "PageAccessPolicyConfiguration.cs"),
            _templateProvider.GetPageAccessPolicyEntityConfigurationTemplate()
        );
        File.WriteAllText(
            Path.Combine(configPath, "WalletAccountConfiguration.cs"),
            _templateProvider.GetWalletAccountEntityConfigurationTemplate()
        );
        File.WriteAllText(
            Path.Combine(configPath, "SellerProfileConfiguration.cs"),
            _templateProvider.GetSellerProfileEntityConfigurationTemplate()
        );

        // Create Services
        var servicesPath = Path.Combine(layerPath, "Services");
        File.WriteAllText(
            Path.Combine(servicesPath, "FileService.cs"),
            _templateProvider.GetFileServiceTemplate()
        );
        File.WriteAllText(
            Path.Combine(servicesPath, "OtpService.cs"),
            _templateProvider.GetOtpServiceTemplate()
        );
        File.WriteAllText(
            Path.Combine(servicesPath, "SmsService.cs"),
            _templateProvider.GetSmsServiceTemplate()
        );

        // Create DependencyInjection
        File.WriteAllText(
            Path.Combine(layerPath, "ServiceCollectionExtensions.cs"),
            _templateProvider.GetInfrastructureExtensionsTemplate()
        );
    }

    private void GenerateTestsLayer(string layerPath, string layerName)
    {
        var csprojContent = _templateProvider.GetTestsCsprojTemplate(layerName);
        File.WriteAllText(Path.Combine(layerPath, $"{layerName}.csproj"), csprojContent);

        var dirs = new[] { "Domain", "Application", "Infrastructure" };
        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(layerPath, dir));
        }
    }
}
