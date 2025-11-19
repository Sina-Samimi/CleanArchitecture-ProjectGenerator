namespace ProjectGenerator.Core.Templates;

public partial class TemplateProvider
{
    // ==================== Application Layer Templates ====================
    
    public string GetApplicationCsprojTemplate()
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>{_namespace}.Application</RootNamespace>
  </PropertyGroup>

    <ItemGroup>
      <PackageReference Include=""MediatR"" Version=""12.2.0"" />
      <PackageReference Include=""FluentValidation"" Version=""11.9.0"" />
      <PackageReference Include=""FluentValidation.DependencyInjectionExtensions"" Version=""11.9.0"" />
      <PackageReference Include=""Microsoft.Extensions.DependencyInjection.Abstractions"" Version=""9.0.0"" />
      <PackageReference Include=""AutoMapper"" Version=""12.0.1"" />
      <PackageReference Include=""AutoMapper.Extensions.Microsoft.DependencyInjection"" Version=""12.0.1"" />
      <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""9.0.0"" />
    </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\Domain\Domain.csproj"" />
  </ItemGroup>

  <ItemGroup>
    <FrameworkReference Include=""Microsoft.AspNetCore.App"" />
  </ItemGroup>

</Project>";
    }

    public string GetMediatRExtensionsTemplate()
    {
        return $@"using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using MediatR;
using FluentValidation;

namespace {_namespace}.Application;

public static class ServiceCollectionExtensions
{{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {{
        var assembly = Assembly.GetExecutingAssembly();
        
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(assembly);
        
        return services;
    }}
}}";
    }

    // ==================== User Management Commands/Queries ====================
    
    public string GetCreateUserCommandTemplate()
    {
        return $@"using MediatR;

namespace {_namespace}.Application.Users.Commands.CreateUser;

public sealed record CreateUserCommand(
    string PhoneNumber,
    string FullName,
    string Email,
    string[] Roles,
    bool IsActive = true) : IRequest<CreateUserResponse>;

public sealed record CreateUserResponse(
    bool Success,
    string? UserId,
    string Message);";
    }

    public string GetCreateUserHandlerTemplate()
    {
        return $@"using MediatR;
using Microsoft.AspNetCore.Identity;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Application.Users.Commands.CreateUser;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public CreateUserHandler(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {{
        _userManager = userManager;
        _roleManager = roleManager;
    }}

    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {{
        var existingUser = await _userManager.FindByNameAsync(request.PhoneNumber);
        if (existingUser != null)
        {{
            return new CreateUserResponse(false, null, ""شماره تلفن قبلاً ثبت شده است."");
        }}

        var user = new ApplicationUser
        {{
            PhoneNumber = request.PhoneNumber,
            UserName = request.PhoneNumber,
            PhoneNumberConfirmed = true,
            FullName = request.FullName,
            Email = request.Email,
            IsActive = request.IsActive,
            CreatedOn = DateTimeOffset.UtcNow
        }};

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {{
            var errors = string.Join("", "", result.Errors.Select(e => e.Description));
            return new CreateUserResponse(false, null, errors);
        }}

        // Assign roles
        foreach (var role in request.Roles)
        {{
            if (await _roleManager.RoleExistsAsync(role))
            {{
                await _userManager.AddToRoleAsync(user, role);
            }}
        }}

        return new CreateUserResponse(true, user.Id, ""کاربر با موفقیت ایجاد شد."");
    }}
}}";
    }

    public string GetUpdateUserCommandTemplate()
    {
        return $@"using MediatR;

namespace {_namespace}.Application.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    string UserId,
    string FullName,
    string? Email,
    string[] Roles,
    bool IsActive) : IRequest<UpdateUserResponse>;

public sealed record UpdateUserResponse(
    bool Success,
    string Message);";
    }

    public string GetUpdateUserHandlerTemplate()
    {
        return $@"using MediatR;
using Microsoft.AspNetCore.Identity;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Application.Users.Commands.UpdateUser;

public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
{{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UpdateUserHandler(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {{
        _userManager = userManager;
        _roleManager = roleManager;
    }}

    public async Task<UpdateUserResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {{
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {{
            return new UpdateUserResponse(false, ""کاربر یافت نشد."");
        }}

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.IsActive = request.IsActive;
        user.LastModifiedOn = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {{
            var errors = string.Join("", "", result.Errors.Select(e => e.Description));
            return new UpdateUserResponse(false, errors);
        }}

        // Update roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        
        foreach (var role in request.Roles)
        {{
            if (await _roleManager.RoleExistsAsync(role))
            {{
                await _userManager.AddToRoleAsync(user, role);
            }}
        }}

        return new UpdateUserResponse(true, ""اطلاعات کاربر با موفقیت به‌روز شد."");
    }}
}}";
    }

    public string GetGetUsersQueryTemplate()
    {
        return $@"using MediatR;

namespace {_namespace}.Application.Users.Queries.GetUsers;

public sealed record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null) : IRequest<GetUsersResponse>;

public sealed record GetUsersResponse(
    List<UserDto> Users,
    int TotalCount,
    int PageNumber,
    int PageSize);

public sealed record UserDto(
    string Id,
    string PhoneNumber,
    string FullName,
    string? Email,
    bool IsActive,
    string[] Roles,
    DateTimeOffset CreatedOn);";
    }

    public string GetGetUsersHandlerTemplate()
    {
        return $@"using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Application.Users.Queries.GetUsers;

public sealed class GetUsersHandler : IRequestHandler<GetUsersQuery, GetUsersResponse>
{{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUsersHandler(UserManager<ApplicationUser> userManager)
    {{
        _userManager = userManager;
    }}

    public async Task<GetUsersResponse> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {{
        var query = _userManager.Users
            .Where(u => !u.IsDeleted)
            .OrderByDescending(u => u.CreatedOn)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {{
            query = query.Where(u =>
                u.PhoneNumber.Contains(request.SearchTerm) ||
                u.FullName.Contains(request.SearchTerm) ||
                (u.Email != null && u.Email.Contains(request.SearchTerm)));
        }}

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {{
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto(
                user.Id,
                user.PhoneNumber ?? string.Empty,
                user.FullName,
                user.Email,
                user.IsActive,
                roles.ToArray(),
                user.CreatedOn));
        }}

        return new GetUsersResponse(userDtos, totalCount, request.PageNumber, request.PageSize);
    }}
}}";
    }

    // ==================== Product Management Commands/Queries ====================
    
    public string GetCreateProductCommandTemplate()
    {
        return $@"using MediatR;
using {_namespace}.Domain.Enums;

namespace {_namespace}.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Summary,
    string Description,
    ProductType Type,
    decimal Price,
    decimal? CompareAtPrice,
    bool TrackInventory,
    int StockQuantity,
    Guid CategoryId,
    string SeoTitle,
    string SeoDescription,
    string SeoKeywords,
    string SeoSlug,
    string? Robots,
    string? FeaturedImagePath,
    string[]? Tags,
    string? DigitalDownloadPath,
    bool IsPublished,
    string? SellerId) : IRequest<CreateProductResponse>;

public sealed record CreateProductResponse(
    bool Success,
    Guid? ProductId,
    string Message);";
    }

    public string GetCreateProductHandlerTemplate()
    {
        return $@"using MediatR;
using {_namespace}.Application.Common.Interfaces;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Application.Products.Commands.CreateProduct;

    public sealed class CreateProductHandler : IRequestHandler<CreateProductCommand, CreateProductResponse>
    {{
        private readonly IApplicationDbContext _context;

        public CreateProductHandler(IApplicationDbContext context)
        {{
        _context = context;
    }}

    public async Task<CreateProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {{
        var category = await _context.Categories.FindAsync(new object[] {{ request.CategoryId }}, cancellationToken);
        if (category == null)
        {{
            return new CreateProductResponse(false, null, ""دسته‌بندی یافت نشد."");
        }}

        var product = new Product(
            request.Name,
            request.Summary,
            request.Description,
            request.Type,
            request.Price,
            request.CompareAtPrice,
            request.TrackInventory,
            request.StockQuantity,
            category,
            request.SeoTitle,
            request.SeoDescription,
            request.SeoKeywords,
            request.SeoSlug,
            request.Robots,
            request.FeaturedImagePath,
            request.Tags,
            request.DigitalDownloadPath,
            request.IsPublished,
            null,
            null,
            request.SellerId);

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateProductResponse(true, product.Id, ""محصول با موفقیت ایجاد شد."");
    }}
}}";
    }

    public string GetGetProductsQueryTemplate()
    {
        return $@"using MediatR;
using {_namespace}.Domain.Enums;

namespace {_namespace}.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    Guid? CategoryId = null,
    bool? IsPublished = null) : IRequest<GetProductsResponse>;

public sealed record GetProductsResponse(
    List<ProductDto> Products,
    int TotalCount,
    int PageNumber,
    int PageSize);

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Summary,
    ProductType Type,
    decimal Price,
    bool IsPublished,
    string CategoryName,
    string? FeaturedImagePath);";
    }

    public string GetGetProductsHandlerTemplate()
    {
        return $@"using MediatR;
using Microsoft.EntityFrameworkCore;
using {_namespace}.Application.Common.Interfaces;

namespace {_namespace}.Application.Products.Queries.GetProducts;

    public sealed class GetProductsHandler : IRequestHandler<GetProductsQuery, GetProductsResponse>
    {{
        private readonly IApplicationDbContext _context;

        public GetProductsHandler(IApplicationDbContext context)
        {{
        _context = context;
    }}

    public async Task<GetProductsResponse> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {{
        var query = _context.Products
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreateDate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {{
            query = query.Where(p =>
                p.Name.Contains(request.SearchTerm) ||
                p.Summary.Contains(request.SearchTerm));
        }}

        if (request.CategoryId.HasValue)
        {{
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }}

        if (request.IsPublished.HasValue)
        {{
            query = query.Where(p => p.IsPublished == request.IsPublished.Value);
        }}

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto(
                p.Id,
                p.Name,
                p.Summary,
                p.Type,
                p.Price,
                p.IsPublished,
                p.Category.Name,
                p.FeaturedImagePath))
            .ToListAsync(cancellationToken);

        return new GetProductsResponse(products, totalCount, request.PageNumber, request.PageSize);
    }}
}}";
    }

    // ==================== Common DTOs ====================
    
    public string GetPaginatedResponseTemplate()
    {
        return $@"namespace {_namespace}.Application.Common;

public record PaginatedResponse<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}}";
    }

    public string GetResultTemplate()
    {
        return $@"namespace {_namespace}.Application.Common;

public record Result(bool Success, string Message)
{{
    public static Result SuccessResult(string message = ""عملیات با موفقیت انجام شد."") 
        => new Result(true, message);
    
    public static Result FailureResult(string message) 
        => new Result(false, message);
}}

public record Result<T>(bool Success, string Message, T? Data) : Result(Success, Message)
{{
    public static Result<T> SuccessResult(T data, string message = ""عملیات با موفقیت انجام شد."") 
        => new Result<T>(true, message, data);
    
    public static new Result<T> FailureResult(string message) 
        => new Result<T>(false, message, default);
}}";
    }

    // ==================== File Upload Services ====================
    
    public string GetIFileServiceTemplate()
    {
        return $@"using Microsoft.AspNetCore.Http;

namespace {_namespace}.Application.Services;

public interface IFileService
{{
    Task<string?> SaveFileAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    string GetFileUrl(string filePath);
}}";
    }

    public string GetISmsServiceTemplate()
    {
        return $@"namespace {_namespace}.Application.Services;

public interface ISmsService
{{
    Task<bool> SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
    Task<bool> SendMessageAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}}";
    }

    public string GetIOtpServiceTemplate()
    {
        return $@"namespace {_namespace}.Application.Services;

public interface IOtpService
{{
    string GenerateOtp();
    Task<bool> StoreOtpAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default);
    Task<bool> ValidateOtpAsync(string phoneNumber, string otp, CancellationToken cancellationToken = default);
}}";
    }

    public string GetApplicationDbContextInterfaceTemplate()
    {
        return $@"using Microsoft.EntityFrameworkCore;
using {_namespace}.Domain.Entities;

namespace {_namespace}.Application.Common.Interfaces;

public interface IApplicationDbContext
{{
    DbSet<Product> Products {{ get; }}
    DbSet<ProductImage> ProductImages {{ get; }}
    DbSet<ProductComment> ProductComments {{ get; }}
    DbSet<ProductExecutionStep> ProductExecutionSteps {{ get; }}
    DbSet<ProductFaq> ProductFaqs {{ get; }}
    DbSet<SiteCategory> Categories {{ get; }}
    DbSet<DiscountCode> DiscountCodes {{ get; }}
    DbSet<Blog> Blogs {{ get; }}
    DbSet<BlogCategory> BlogCategories {{ get; }}
    DbSet<BlogAuthor> BlogAuthors {{ get; }}
    DbSet<BlogComment> BlogComments {{ get; }}
    DbSet<Invoice> Invoices {{ get; }}
    DbSet<InvoiceItem> InvoiceItems {{ get; }}
    DbSet<Transaction> Transactions {{ get; }}
    DbSet<WalletAccount> WalletAccounts {{ get; }}
    DbSet<WalletTransaction> WalletTransactions {{ get; }}
    DbSet<SiteSetting> SiteSettings {{ get; }}
    DbSet<NavigationMenuItem> NavigationMenuItems {{ get; }}
    DbSet<FinancialSettings> FinancialSettings {{ get; }}
    DbSet<SellerProfile> SellerProfiles {{ get; }}
    DbSet<AccessPermission> AccessPermissions {{ get; }}
    DbSet<PageAccessPolicy> PageAccessPolicies {{ get; }}
    DbSet<UserSession> UserSessions {{ get; }}

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}}";
    }


    public string GetProductDtosTemplate()
    {
        return $@"using System;
using System.ComponentModel.DataAnnotations;

namespace {_namespace}.Application.DTOs.Product;

public class ProductDto
{{
    public int Id {{ get; set; }}
    public string Name {{ get; set; }} = string.Empty;
    public string? Summary {{ get; set; }}
    public string? Description {{ get; set; }}
    public decimal Price {{ get; set; }}
    public decimal? CompareAtPrice {{ get; set; }}
    public bool TrackInventory {{ get; set; }}
    public int StockQuantity {{ get; set; }}
    public bool IsPublished {{ get; set; }}
    public int CategoryId {{ get; set; }}
    public int? SellerId {{ get; set; }}
    public string? FeaturedImageUrl {{ get; set; }}
    public string? SeoTitle {{ get; set; }}
    public string? SeoDescription {{ get; set; }}
    public string? SeoSlug {{ get; set; }}
}}

public class CreateProductDto
{{
    [Required, MaxLength(200)]
    public string Name {{ get; set; }} = string.Empty;
    [MaxLength(500)]
    public string? Summary {{ get; set; }}
    public string? Description {{ get; set; }}
    [Range(0, double.MaxValue)]
    public decimal Price {{ get; set; }}
    [Range(0, double.MaxValue)]
    public decimal? CompareAtPrice {{ get; set; }}
    public bool TrackInventory {{ get; set; }} = true;
    [Range(0, int.MaxValue)]
    public int StockQuantity {{ get; set; }} = 0;
    public bool IsPublished {{ get; set; }} = true;
    [Required]
    public int CategoryId {{ get; set; }}
    public int SellerId {{ get; set; }}
    public string? FeaturedImageUrl {{ get; set; }}
    public string? SeoTitle {{ get; set; }}
    public string? SeoDescription {{ get; set; }}
    public string? SeoSlug {{ get; set; }}
}}

public class UpdateProductDto : CreateProductDto
{{
    public int Id {{ get; set; }}
}}";
    }

    public string GetCategoryDtosTemplate()
    {
        return $@"using System.ComponentModel.DataAnnotations;

namespace {_namespace}.Application.DTOs.Category;

public class CategoryDto
{{
    public int Id {{ get; set; }}
    public string Name {{ get; set; }} = string.Empty;
    public string Slug {{ get; set; }} = string.Empty;
    public string? Description {{ get; set; }}
    public int? ParentId {{ get; set; }}
}}

public class CreateCategoryDto
{{
    [Required, MaxLength(200)]
    public string Name {{ get; set; }} = string.Empty;
    [MaxLength(200)]
    public string? Slug {{ get; set; }}
    [MaxLength(500)]
    public string? Description {{ get; set; }}
    public int? ParentId {{ get; set; }}
}}";
    }

    public string GetBlogDtosTemplate()
    {
        return $@"using System.ComponentModel.DataAnnotations;

namespace {_namespace}.Application.DTOs.Blog;

public class BlogPostDto
{{
    public int Id {{ get; set; }}
    public string Title {{ get; set; }} = string.Empty;
    public string Slug {{ get; set; }} = string.Empty;
    public string Summary {{ get; set; }} = string.Empty;
    public string Content {{ get; set; }} = string.Empty;
    public bool IsPublished {{ get; set; }}
    public DateTimeOffset? PublishedAt {{ get; set; }}
}}

public class CreateBlogDto
{{
    [Required, MaxLength(200)]
    public string Title {{ get; set; }} = string.Empty;
    [MaxLength(200)]
    public string Slug {{ get; set; }} = string.Empty;
    [MaxLength(500)]
    public string Summary {{ get; set; }} = string.Empty;
    [Required]
    public string Content {{ get; set; }} = string.Empty;
    public bool IsPublished {{ get; set; }}
}}";
    }

    public string GetOrderDtosTemplate()
    {
        return $@"using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace {_namespace}.Application.DTOs.Order;

public class OrderDto
{{
    public int Id {{ get; set; }}
    public int UserId {{ get; set; }}
    public decimal TotalAmount {{ get; set; }}
    public string Status {{ get; set; }} = ""Pending"";
    public DateTimeOffset CreatedAt {{ get; set; }} = DateTimeOffset.UtcNow;
    public List<OrderItemDto> Items {{ get; set; }} = new();
}}

public class OrderItemDto
{{
    public int ProductId {{ get; set; }}
    public string ProductName {{ get; set; }} = string.Empty;
    public decimal UnitPrice {{ get; set; }}
    public int Quantity {{ get; set; }}
}}

public class CreateOrderDto
{{
    public int UserId {{ get; set; }}
    [Required]
    public string ShippingAddress {{ get; set; }} = string.Empty;
    [Required]
    public string PaymentMethod {{ get; set; }} = ""Online"";
    [MinLength(1)]
    public List<CreateOrderItemDto> Items {{ get; set; }} = new();
}}

public class CreateOrderItemDto
{{
    public int ProductId {{ get; set; }}
    public string ProductName {{ get; set; }} = string.Empty;
    public decimal UnitPrice {{ get; set; }}
    public int Quantity {{ get; set; }}
}}";
    }

    public string GetCartDtosTemplate()
    {
        return $@"using System.Collections.Generic;
using System.Linq;

namespace {_namespace}.Application.DTOs.Cart;

public class CartDto
{{
    public int UserId {{ get; set; }}
    public List<CartItemDto> Items {{ get; set; }} = new();
    public decimal TotalAmount => Items.Sum(item => item.TotalPrice);
}}

public class CartItemDto
{{
    public int ProductId {{ get; set; }}
    public string ProductName {{ get; set; }} = string.Empty;
    public decimal UnitPrice {{ get; set; }}
    public int Quantity {{ get; set; }}
    public decimal TotalPrice => UnitPrice * Quantity;
}}";
    }

    public string GetProductServiceInterfaceTemplate()
    {
        return $@"using {_namespace}.Application.Common;
using {_namespace}.Application.DTOs.Product;

namespace {_namespace}.Application.Interfaces;

public interface IProductService
{{
    Task<Result<List<ProductDto>>> GetAllAsync();
    Task<Result<ProductDto>> GetByIdAsync(int id);
    Task<Result<List<ProductDto>>> GetByCategoryIdAsync(int categoryId);
    Task<Result<List<ProductDto>>> GetBySellerIdAsync(int sellerId);
    Task<Result<ProductDto>> CreateAsync(CreateProductDto dto);
    Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductDto dto);
    Task<Result> DeleteAsync(int id);
}}";
    }

    public string GetCategoryServiceInterfaceTemplate()
    {
        return $@"using {_namespace}.Application.Common;
using {_namespace}.Application.DTOs.Category;

namespace {_namespace}.Application.Interfaces;

public interface ICategoryService
{{
    Task<Result<List<CategoryDto>>> GetAllAsync();
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto);
}}";
    }

    public string GetOrderServiceInterfaceTemplate()
    {
        return $@"using {_namespace}.Application.Common;
using {_namespace}.Application.DTOs.Order;

namespace {_namespace}.Application.Interfaces;

public interface IOrderService
{{
    Task<Result<OrderDto>> CreateAsync(CreateOrderDto dto);
    Task<Result<OrderDto>> GetByIdAsync(int id);
    Task<Result<List<OrderDto>>> GetByUserIdAsync(int userId);
}}";
    }

    public string GetBlogServiceInterfaceTemplate()
    {
        return $@"using {_namespace}.Application.Common;
using {_namespace}.Application.DTOs.Blog;

namespace {_namespace}.Application.Interfaces;

public interface IBlogService
{{
    Task<Result> CreateAsync(CreateBlogDto dto);
    Task<Result<List<BlogPostDto>>> GetAllPublishedAsync();
    Task<Result<BlogPostDto>> GetBySlugAsync(string slug);
}}";
    }

    public string GetCartServiceInterfaceTemplate()
    {
        return $@"using {_namespace}.Application.Common;
using {_namespace}.Application.DTOs.Cart;

namespace {_namespace}.Application.Interfaces;

public interface ICartService
{{
    Task<Result<CartDto>> GetByUserIdAsync(int userId);
    Task<Result> AddItemAsync(int userId, int productId, int quantity);
    Task<Result> RemoveItemAsync(int userId, int productId);
    Task ClearCartAsync(int userId);
}}";
    }

}
