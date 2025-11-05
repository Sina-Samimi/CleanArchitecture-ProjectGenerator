namespace ProjectGenerator.Templates;

public partial class TemplateProvider
{
    // Product Service Interface
    public string GetIProductServiceTemplate()
    {
        return $@"using {_namespace}.Application.DTOs.Product;
using {_namespace}.SharedKernel.Results;

namespace {_namespace}.Application.Interfaces;

public interface IProductService
{{
    Task<Result<ProductDto>> GetByIdAsync(int id);
    Task<Result<IEnumerable<ProductDto>>> GetAllAsync();
    Task<Result<IEnumerable<ProductDto>>> GetBySellerIdAsync(int sellerId);
    Task<Result<IEnumerable<ProductDto>>> GetByCategoryIdAsync(int categoryId);
    Task<Result<ProductDto>> CreateAsync(CreateProductDto dto);
    Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductDto dto);
    Task<Result> DeleteAsync(int id);
    Task<Result<IEnumerable<ProductDto>>> SearchAsync(string keyword);
}}
";
    }

    // Product DTOs
    public string GetProductDtosTemplate()
    {
        return $@"namespace {_namespace}.Application.DTOs.Product;

public class ProductDto
{{
    public int Id {{ get; set; }}
    public string Title {{ get; set; }} = string.Empty;
    public string Description {{ get; set; }} = string.Empty;
    public string? ShortDescription {{ get; set; }}
    public decimal Price {{ get; set; }}
    public decimal? DiscountPrice {{ get; set; }}
    public int Stock {{ get; set; }}
    public string? ImageUrl {{ get; set; }}
    public string Slug {{ get; set; }} = string.Empty;
    public bool IsActive {{ get; set; }}
    public bool IsFeatured {{ get; set; }}
    public int ViewCount {{ get; set; }}
    public int SellerId {{ get; set; }}
    public string? SellerName {{ get; set; }}
    public int CategoryId {{ get; set; }}
    public string? CategoryName {{ get; set; }}
}}

public class CreateProductDto
{{
    public string Title {{ get; set; }} = string.Empty;
    public string Description {{ get; set; }} = string.Empty;
    public string? ShortDescription {{ get; set; }}
    public decimal Price {{ get; set; }}
    public decimal? DiscountPrice {{ get; set; }}
    public int Stock {{ get; set; }}
    public string? ImageUrl {{ get; set; }}
    public int SellerId {{ get; set; }}
    public int CategoryId {{ get; set; }}
}}

public class UpdateProductDto
{{
    public string Title {{ get; set; }} = string.Empty;
    public string Description {{ get; set; }} = string.Empty;
    public string? ShortDescription {{ get; set; }}
    public decimal Price {{ get; set; }}
    public decimal? DiscountPrice {{ get; set; }}
    public int Stock {{ get; set; }}
    public string? ImageUrl {{ get; set; }}
    public int CategoryId {{ get; set; }}
    public bool IsActive {{ get; set; }}
    public bool IsFeatured {{ get; set; }}
}}
";
    }

    // Order Service Interface
    public string GetIOrderServiceTemplate()
    {
        return $@"using {_namespace}.Application.DTOs.Order;
using {_namespace}.SharedKernel.Results;

namespace {_namespace}.Application.Interfaces;

public interface IOrderService
{{
    Task<Result<OrderDto>> GetByIdAsync(int id);
    Task<Result<IEnumerable<OrderDto>>> GetByUserIdAsync(int userId);
    Task<Result<OrderDto>> CreateAsync(CreateOrderDto dto);
    Task<Result> UpdateStatusAsync(int id, int status);
    Task<Result> CancelOrderAsync(int id);
}}
";
    }

    // Order DTOs
    public string GetOrderDtosTemplate()
    {
        return $@"namespace {_namespace}.Application.DTOs.Order;

public class OrderDto
{{
    public int Id {{ get; set; }}
    public string OrderNumber {{ get; set; }} = string.Empty;
    public int UserId {{ get; set; }}
    public DateTime OrderDate {{ get; set; }}
    public int Status {{ get; set; }}
    public string StatusName {{ get; set; }} = string.Empty;
    public decimal TotalAmount {{ get; set; }}
    public decimal DiscountAmount {{ get; set; }}
    public decimal ShippingCost {{ get; set; }}
    public decimal FinalAmount {{ get; set; }}
    public string ShippingFullName {{ get; set; }} = string.Empty;
    public string ShippingPhoneNumber {{ get; set; }} = string.Empty;
    public string ShippingAddress {{ get; set; }} = string.Empty;
    public string ShippingCity {{ get; set; }} = string.Empty;
    public string? ShippingPostalCode {{ get; set; }}
    public List<OrderItemDto> Items {{ get; set; }} = new();
}}

public class OrderItemDto
{{
    public int ProductId {{ get; set; }}
    public string ProductTitle {{ get; set; }} = string.Empty;
    public decimal UnitPrice {{ get; set; }}
    public int Quantity {{ get; set; }}
    public decimal TotalPrice {{ get; set; }}
}}

public class CreateOrderDto
{{
    public int UserId {{ get; set; }}
    public string ShippingFullName {{ get; set; }} = string.Empty;
    public string ShippingPhoneNumber {{ get; set; }} = string.Empty;
    public string ShippingAddress {{ get; set; }} = string.Empty;
    public string ShippingCity {{ get; set; }} = string.Empty;
    public string? ShippingPostalCode {{ get; set; }}
    public List<CreateOrderItemDto> Items {{ get; set; }} = new();
}}

public class CreateOrderItemDto
{{
    public int ProductId {{ get; set; }}
    public int Quantity {{ get; set; }}
}}
";
    }

    // Cart Service Interface
    public string GetICartServiceTemplate()
    {
        return $@"using {_namespace}.Application.DTOs.Cart;
using {_namespace}.SharedKernel.Results;

namespace {_namespace}.Application.Interfaces;

public interface ICartService
{{
    Task<Result<CartDto>> GetByUserIdAsync(int userId);
    Task<Result> AddItemAsync(int userId, int productId, int quantity);
    Task<Result> UpdateItemQuantityAsync(int userId, int productId, int quantity);
    Task<Result> RemoveItemAsync(int userId, int productId);
    Task<Result> ClearCartAsync(int userId);
}}
";
    }

    // Cart DTOs
    public string GetCartDtosTemplate()
    {
        return $@"namespace {_namespace}.Application.DTOs.Cart;

public class CartDto
{{
    public int Id {{ get; set; }}
    public int UserId {{ get; set; }}
    public List<CartItemDto> Items {{ get; set; }} = new();
    public decimal TotalAmount {{ get; set; }}
}}

public class CartItemDto
{{
    public int ProductId {{ get; set; }}
    public string ProductTitle {{ get; set; }} = string.Empty;
    public decimal Price {{ get; set; }}
    public int Quantity {{ get; set; }}
    public decimal TotalPrice {{ get; set; }}
    public string? ImageUrl {{ get; set; }}
}}
";
    }

    // Blog Service Interface
    public string GetIBlogServiceTemplate()
    {
        return $@"using {_namespace}.Application.DTOs.Blog;
using {_namespace}.SharedKernel.Results;

namespace {_namespace}.Application.Interfaces;

public interface IBlogService
{{
    Task<Result<BlogDto>> GetByIdAsync(int id);
    Task<Result<BlogDto>> GetBySlugAsync(string slug);
    Task<Result<IEnumerable<BlogDto>>> GetAllPublishedAsync();
    Task<Result<IEnumerable<BlogDto>>> GetByAuthorIdAsync(int authorId);
    Task<Result<BlogDto>> CreateAsync(CreateBlogDto dto);
    Task<Result<BlogDto>> UpdateAsync(int id, UpdateBlogDto dto);
    Task<Result> DeleteAsync(int id);
    Task<Result> PublishAsync(int id);
}}
";
    }

    // Blog DTOs
    public string GetBlogDtosTemplate()
    {
        return $@"namespace {_namespace}.Application.DTOs.Blog;

public class BlogDto
{{
    public int Id {{ get; set; }}
    public string Title {{ get; set; }} = string.Empty;
    public string Content {{ get; set; }} = string.Empty;
    public string? Summary {{ get; set; }}
    public string Slug {{ get; set; }} = string.Empty;
    public string? FeaturedImageUrl {{ get; set; }}
    public int AuthorId {{ get; set; }}
    public string? AuthorName {{ get; set; }}
    public int Status {{ get; set; }}
    public string StatusName {{ get; set; }} = string.Empty;
    public int ViewCount {{ get; set; }}
    public DateTime? PublishedDate {{ get; set; }}
    public DateTime CreatedDate {{ get; set; }}
}}

public class CreateBlogDto
{{
    public string Title {{ get; set; }} = string.Empty;
    public string Content {{ get; set; }} = string.Empty;
    public string? Summary {{ get; set; }}
    public string? FeaturedImageUrl {{ get; set; }}
    public int AuthorId {{ get; set; }}
}}

public class UpdateBlogDto
{{
    public string Title {{ get; set; }} = string.Empty;
    public string Content {{ get; set; }} = string.Empty;
    public string? Summary {{ get; set; }}
    public string? FeaturedImageUrl {{ get; set; }}
    public bool AllowComments {{ get; set; }}
}}
";
    }

    // Category Service Interface
    public string GetICategoryServiceTemplate()
    {
        return $@"using {_namespace}.Application.DTOs.Category;
using {_namespace}.SharedKernel.Results;

namespace {_namespace}.Application.Interfaces;

public interface ICategoryService
{{
    Task<Result<CategoryDto>> GetByIdAsync(int id);
    Task<Result<IEnumerable<CategoryDto>>> GetAllAsync();
    Task<Result<IEnumerable<CategoryDto>>> GetRootCategoriesAsync();
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto);
    Task<Result<CategoryDto>> UpdateAsync(int id, UpdateCategoryDto dto);
    Task<Result> DeleteAsync(int id);
}}
";
    }

    // Category DTOs
    public string GetCategoryDtosTemplate()
    {
        return $@"namespace {_namespace}.Application.DTOs.Category;

public class CategoryDto
{{
    public int Id {{ get; set; }}
    public string Name {{ get; set; }} = string.Empty;
    public string? Description {{ get; set; }}
    public string Slug {{ get; set; }} = string.Empty;
    public int? ParentCategoryId {{ get; set; }}
    public string? ParentCategoryName {{ get; set; }}
    public bool IsActive {{ get; set; }}
    public List<CategoryDto> SubCategories {{ get; set; }} = new();
}}

public class CreateCategoryDto
{{
    public string Name {{ get; set; }} = string.Empty;
    public string? Description {{ get; set; }}
    public int? ParentCategoryId {{ get; set; }}
}}

public class UpdateCategoryDto
{{
    public string Name {{ get; set; }} = string.Empty;
    public string? Description {{ get; set; }}
    public int? ParentCategoryId {{ get; set; }}
    public bool IsActive {{ get; set; }}
}}
";
    }

    // Invoice Service Interface
    public string GetIInvoiceServiceTemplate()
    {
        return $@"using {_namespace}.Application.DTOs.Invoice;
using {_namespace}.SharedKernel.Results;

namespace {_namespace}.Application.Interfaces;

public interface IInvoiceService
{{
    Task<Result<InvoiceDto>> GetByIdAsync(int id);
    Task<Result<InvoiceDto>> GetByOrderIdAsync(int orderId);
    Task<Result<IEnumerable<InvoiceDto>>> GetByUserIdAsync(int userId);
    Task<Result<InvoiceDto>> CreateAsync(CreateInvoiceDto dto);
    Task<Result> MarkAsPaidAsync(int id, string paymentMethod, string transactionId);
}}
";
    }

    // Invoice DTOs
    public string GetInvoiceDtosTemplate()
    {
        return $@"namespace {_namespace}.Application.DTOs.Invoice;

public class InvoiceDto
{{
    public int Id {{ get; set; }}
    public string InvoiceNumber {{ get; set; }} = string.Empty;
    public int OrderId {{ get; set; }}
    public string OrderNumber {{ get; set; }} = string.Empty;
    public int UserId {{ get; set; }}
    public DateTime IssueDate {{ get; set; }}
    public DateTime? DueDate {{ get; set; }}
    public int Status {{ get; set; }}
    public string StatusName {{ get; set; }} = string.Empty;
    public decimal TotalAmount {{ get; set; }}
    public decimal PaidAmount {{ get; set; }}
    public decimal RemainingAmount {{ get; set; }}
    public string? PaymentMethod {{ get; set; }}
    public string? TransactionId {{ get; set; }}
    public DateTime? PaymentDate {{ get; set; }}
}}

public class CreateInvoiceDto
{{
    public int OrderId {{ get; set; }}
    public int UserId {{ get; set; }}
    public DateTime? DueDate {{ get; set; }}
}}
";
    }
}
