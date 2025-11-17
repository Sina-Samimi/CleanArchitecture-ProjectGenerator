namespace ProjectGenerator.Templates;

public partial class TemplateProvider
{
    // Product Entity
    public string GetProductEntityTemplate()
    {
        return $@"using {_namespace}.Domain.Enums;

namespace {_namespace}.Domain.Entities;

public class Product : BaseEntity, IAggregateRoot
{{
    public string Title {{ get; set; }} = string.Empty;
    public string Description {{ get; set; }} = string.Empty;
    public string? ShortDescription {{ get; set; }}
    public decimal Price {{ get; set; }}
    public decimal? DiscountPrice {{ get; set; }}
    public int Stock {{ get; set; }}
    public string? ImageUrl {{ get; set; }}
    public string Slug {{ get; set; }} = string.Empty;
    public bool IsActive {{ get; set; }} = true;
    public bool IsFeatured {{ get; set; }} = false;
    public int ViewCount {{ get; set; }} = 0;
    public int SellerId {{ get; set; }}
    public int CategoryId {{ get; set; }}
    
    // Navigation properties
    // Note: ApplicationUser navigation property is configured in Infrastructure layer
    public virtual Category? Category {{ get; set; }}
    public virtual ICollection<ProductImage> ProductImages {{ get; set; }} = new List<ProductImage>();
    public virtual ICollection<OrderItem> OrderItems {{ get; set; }} = new List<OrderItem>();
}}
";
    }

    // Category Entity
    public string GetCategoryEntityTemplate()
    {
        return $@"namespace {_namespace}.Domain.Entities;

public class Category : BaseEntity
{{
    public string Name {{ get; set; }} = string.Empty;
    public string? Description {{ get; set; }}
    public string Slug {{ get; set; }} = string.Empty;
    public int? ParentCategoryId {{ get; set; }}
    public bool IsActive {{ get; set; }} = true;
    
    // Navigation properties
    public virtual Category? ParentCategory {{ get; set; }}
    public virtual ICollection<Category> SubCategories {{ get; set; }} = new List<Category>();
    public virtual ICollection<Product> Products {{ get; set; }} = new List<Product>();
}}
";
    }

    // ProductImage Entity
    public string GetProductImageEntityTemplate()
    {
        return $@"namespace {_namespace}.Domain.Entities;

public class ProductImage : BaseEntity
{{
    public string ImageUrl {{ get; set; }} = string.Empty;
    public string? Alt {{ get; set; }}
    public bool IsMainImage {{ get; set; }} = false;
    public int DisplayOrder {{ get; set; }} = 0;
    public int ProductId {{ get; set; }}
    
    // Navigation properties
    public virtual Product? Product {{ get; set; }}
}}
";
    }

    // Order Entity
    public string GetOrderEntityTemplate()
    {
        return $@"using {_namespace}.Domain.Enums;

namespace {_namespace}.Domain.Entities;

public class Order : BaseEntity, IAggregateRoot
{{
    public string OrderNumber {{ get; set; }} = string.Empty;
    public int UserId {{ get; set; }}
    public DateTime OrderDate {{ get; set; }} = DateTime.UtcNow;
    public OrderStatus Status {{ get; set; }} = OrderStatus.Pending;
    public decimal TotalAmount {{ get; set; }}
    public decimal DiscountAmount {{ get; set; }} = 0;
    public decimal ShippingCost {{ get; set; }} = 0;
    public decimal FinalAmount {{ get; set; }}
    public string? Notes {{ get; set; }}
    
    // Shipping Address
    public string ShippingFullName {{ get; set; }} = string.Empty;
    public string ShippingPhoneNumber {{ get; set; }} = string.Empty;
    public string ShippingAddress {{ get; set; }} = string.Empty;
    public string ShippingCity {{ get; set; }} = string.Empty;
    public string? ShippingPostalCode {{ get; set; }}
    
    // Navigation properties
    // Note: ApplicationUser navigation property is configured in Infrastructure layer
    public virtual ICollection<OrderItem> OrderItems {{ get; set; }} = new List<OrderItem>();
    public virtual Invoice? Invoice {{ get; set; }}
}}
";
    }

    // OrderItem Entity
    public string GetOrderItemEntityTemplate()
    {
        return $@"namespace {_namespace}.Domain.Entities;

public class OrderItem : BaseEntity
{{
    public int OrderId {{ get; set; }}
    public int ProductId {{ get; set; }}
    public string ProductTitle {{ get; set; }} = string.Empty;
    public decimal UnitPrice {{ get; set; }}
    public int Quantity {{ get; set; }}
    public decimal TotalPrice {{ get; set; }}
    
    // Navigation properties
    public virtual Order? Order {{ get; set; }}
    public virtual Product? Product {{ get; set; }}
}}
";
    }

    // Invoice Entity
    public string GetInvoiceEntityTemplate()
    {
        return $@"using {_namespace}.Domain.Enums;

namespace {_namespace}.Domain.Entities;

public class Invoice : BaseEntity, IAggregateRoot
{{
    public string InvoiceNumber {{ get; set; }} = string.Empty;
    public int OrderId {{ get; set; }}
    public int UserId {{ get; set; }}
    public DateTime IssueDate {{ get; set; }} = DateTime.UtcNow;
    public DateTime? DueDate {{ get; set; }}
    public InvoiceStatus Status {{ get; set; }} = InvoiceStatus.Unpaid;
    public decimal TotalAmount {{ get; set; }}
    public decimal PaidAmount {{ get; set; }} = 0;
    public decimal RemainingAmount {{ get; set; }}
    public string? PaymentMethod {{ get; set; }}
    public string? TransactionId {{ get; set; }}
    public DateTime? PaymentDate {{ get; set; }}
    
    // Navigation properties
    public virtual Order? Order {{ get; set; }}
    // Note: ApplicationUser navigation property is configured in Infrastructure layer
}}
";
    }

    // Cart Entity
    public string GetCartEntityTemplate()
    {
        return $@"namespace {_namespace}.Domain.Entities;

public class Cart : BaseEntity
{{
    public int UserId {{ get; set; }}
    public DateTime LastUpdated {{ get; set; }} = DateTime.UtcNow;
    
    // Navigation properties
    // Note: ApplicationUser navigation property is configured in Infrastructure layer
    public virtual ICollection<CartItem> CartItems {{ get; set; }} = new List<CartItem>();
}}
";
    }

    // CartItem Entity
    public string GetCartItemEntityTemplate()
    {
        return $@"namespace {_namespace}.Domain.Entities;

public class CartItem : BaseEntity
{{
    public int CartId {{ get; set; }}
    public int ProductId {{ get; set; }}
    public int Quantity {{ get; set; }}
    public DateTime AddedDate {{ get; set; }} = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Cart? Cart {{ get; set; }}
    public virtual Product? Product {{ get; set; }}
}}
";
    }

    // Blog Entity
    public string GetBlogEntityTemplate()
    {
        return $@"using {_namespace}.Domain.Enums;

namespace {_namespace}.Domain.Entities;

public class Blog : BaseEntity, IAggregateRoot
{{
    public string Title {{ get; set; }} = string.Empty;
    public string Content {{ get; set; }} = string.Empty;
    public string? Summary {{ get; set; }}
    public string Slug {{ get; set; }} = string.Empty;
    public string? FeaturedImageUrl {{ get; set; }}
    public int AuthorId {{ get; set; }}
    public BlogStatus Status {{ get; set; }} = BlogStatus.Draft;
    public bool AllowComments {{ get; set; }} = true;
    public int ViewCount {{ get; set; }} = 0;
    public DateTime? PublishedDate {{ get; set; }}
    public string? MetaTitle {{ get; set; }}
    public string? MetaDescription {{ get; set; }}
    public string? MetaKeywords {{ get; set; }}
    
    // Navigation properties
    // Note: ApplicationUser navigation property is configured in Infrastructure layer
    public virtual ICollection<BlogComment> Comments {{ get; set; }} = new List<BlogComment>();
    public virtual ICollection<BlogCategory> BlogCategories {{ get; set; }} = new List<BlogCategory>();
}}
";
    }

    // BlogComment Entity
    public string GetBlogCommentEntityTemplate()
    {
        return $@"using {_namespace}.Domain.Enums;

namespace {_namespace}.Domain.Entities;

public class BlogComment : BaseEntity
{{
    public int BlogId {{ get; set; }}
    public int? UserId {{ get; set; }}
    public string AuthorName {{ get; set; }} = string.Empty;
    public string? AuthorEmail {{ get; set; }}
    public string Content {{ get; set; }} = string.Empty;
    public CommentStatus Status {{ get; set; }} = CommentStatus.Pending;
    public int? ParentCommentId {{ get; set; }}
    
    // Navigation properties
    public virtual Blog? Blog {{ get; set; }}
    // Note: ApplicationUser navigation property is configured in Infrastructure layer
    public virtual BlogComment? ParentComment {{ get; set; }}
    public virtual ICollection<BlogComment> Replies {{ get; set; }} = new List<BlogComment>();
}}
";
    }

    // BlogCategory Entity (for many-to-many relationship)
    public string GetBlogCategoryEntityTemplate()
    {
        return $@"namespace {_namespace}.Domain.Entities;

public class BlogCategory : BaseEntity
{{
    public string Name {{ get; set; }} = string.Empty;
    public string? Description {{ get; set; }}
    public string Slug {{ get; set; }} = string.Empty;
    public bool IsActive {{ get; set; }} = true;
    
    // Navigation properties
    public virtual ICollection<Blog> Blogs {{ get; set; }} = new List<Blog>();
}}
";
    }

    // Enums
    public string GetOrderStatusEnumTemplate()
    {
        return $@"namespace {_namespace}.Domain.Enums;

public enum OrderStatus
{{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4,
    Refunded = 5
}}
";
    }

    public string GetInvoiceStatusEnumTemplate()
    {
        return $@"namespace {_namespace}.Domain.Enums;

public enum InvoiceStatus
{{
    Unpaid = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4
}}
";
    }

    public string GetBlogStatusEnumTemplate()
    {
        return $@"namespace {_namespace}.Domain.Enums;

public enum BlogStatus
{{
    Draft = 0,
    Published = 1,
    Archived = 2
}}
";
    }

    public string GetCommentStatusEnumTemplate()
    {
        return $@"namespace {_namespace}.Domain.Enums;

public enum CommentStatus
{{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Spam = 3
}}
";
    }
}
