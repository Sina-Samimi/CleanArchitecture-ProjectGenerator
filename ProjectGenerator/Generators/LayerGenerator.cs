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
        var dirs = new[] { "Entities", "Enums", "ValueObjects", "Events" };
        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(layerPath, dir));
        }

        // Create base entity
        var baseEntityPath = Path.Combine(layerPath, "Entities");
        File.WriteAllText(
            Path.Combine(baseEntityPath, "BaseEntity.cs"),
            _templateProvider.GetBaseEntityTemplate()
        );

        File.WriteAllText(
            Path.Combine(baseEntityPath, "IAggregateRoot.cs"),
            _templateProvider.GetIAggregateRootTemplate()
        );

        // Generate all domain entities based on features
        var features = _config.Options.Features;

        if (features.ProductCatalog)
        {
            File.WriteAllText(
                Path.Combine(baseEntityPath, "Product.cs"),
                _templateProvider.GetProductEntityTemplate()
            );
            File.WriteAllText(
                Path.Combine(baseEntityPath, "Category.cs"),
                _templateProvider.GetCategoryEntityTemplate()
            );
            File.WriteAllText(
                Path.Combine(baseEntityPath, "ProductImage.cs"),
                _templateProvider.GetProductImageEntityTemplate()
            );
        }

        if (features.ShoppingCart)
        {
            File.WriteAllText(
                Path.Combine(baseEntityPath, "Cart.cs"),
                _templateProvider.GetCartEntityTemplate()
            );
            File.WriteAllText(
                Path.Combine(baseEntityPath, "CartItem.cs"),
                _templateProvider.GetCartItemEntityTemplate()
            );
        }

        if (features.Invoicing)
        {
            File.WriteAllText(
                Path.Combine(baseEntityPath, "Order.cs"),
                _templateProvider.GetOrderEntityTemplate()
            );
            File.WriteAllText(
                Path.Combine(baseEntityPath, "OrderItem.cs"),
                _templateProvider.GetOrderItemEntityTemplate()
            );
            File.WriteAllText(
                Path.Combine(baseEntityPath, "Invoice.cs"),
                _templateProvider.GetInvoiceEntityTemplate()
            );
        }

        if (features.BlogSystem)
        {
            File.WriteAllText(
                Path.Combine(baseEntityPath, "Blog.cs"),
                _templateProvider.GetBlogEntityTemplate()
            );
            File.WriteAllText(
                Path.Combine(baseEntityPath, "BlogComment.cs"),
                _templateProvider.GetBlogCommentEntityTemplate()
            );
            File.WriteAllText(
                Path.Combine(baseEntityPath, "BlogCategory.cs"),
                _templateProvider.GetBlogCategoryEntityTemplate()
            );
        }

        // Generate Enums
        var enumsPath = Path.Combine(layerPath, "Enums");
        if (features.Invoicing)
        {
            File.WriteAllText(
                Path.Combine(enumsPath, "OrderStatus.cs"),
                _templateProvider.GetOrderStatusEnumTemplate()
            );
            File.WriteAllText(
                Path.Combine(enumsPath, "InvoiceStatus.cs"),
                _templateProvider.GetInvoiceStatusEnumTemplate()
            );
        }

        if (features.BlogSystem)
        {
            File.WriteAllText(
                Path.Combine(enumsPath, "BlogStatus.cs"),
                _templateProvider.GetBlogStatusEnumTemplate()
            );
            File.WriteAllText(
                Path.Combine(enumsPath, "CommentStatus.cs"),
                _templateProvider.GetCommentStatusEnumTemplate()
            );
        }
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
        var csprojContent = _templateProvider.GetInfrastructureCsprojTemplate(layerName);
        File.WriteAllText(Path.Combine(layerPath, $"{layerName}.csproj"), csprojContent);

        var dirs = new[] { "Data", "Repositories", "Services", "Identity" };
        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(Path.Combine(layerPath, dir));
        }

        // Create DbContext template (enhanced with all entities)
        var dataPath = Path.Combine(layerPath, "Data");
        File.WriteAllText(
            Path.Combine(dataPath, "ApplicationDbContext.cs"),
            _templateProvider.GetDbContextEnhancedTemplate()
        );

        // Create GenericRepository
        var repoPath = Path.Combine(layerPath, "Repositories");
        File.WriteAllText(
            Path.Combine(repoPath, "GenericRepository.cs"),
            _templateProvider.GetGenericRepositoryTemplate()
        );

        // Create DependencyInjection
        File.WriteAllText(
            Path.Combine(layerPath, "DependencyInjection.cs"),
            _templateProvider.GetInfrastructureDependencyInjectionTemplate()
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
