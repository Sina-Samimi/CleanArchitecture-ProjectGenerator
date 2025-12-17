using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Catalog;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Commands.Catalog;

public sealed record CreateProductCommand(
    string Name,
    string Summary,
    string Description,
    ProductType Type,
    decimal? Price,
    decimal? CompareAtPrice,
    bool TrackInventory,
    int StockQuantity,
    Guid CategoryId,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    string SeoTitle,
    string SeoDescription,
    string SeoKeywords,
    string SeoSlug,
    string Robots,
    string? FeaturedImagePath,
    string? DigitalDownloadPath,
    IReadOnlyCollection<string>? Tags,
    IReadOnlyCollection<CreateProductCommand.ProductGalleryItem>? Gallery,
    string? SellerId,
    bool IsCustomOrder = false,
    string? Brand = null) : ICommand<Guid>
{
    public sealed record ProductGalleryItem(string Path, int Order);

    public sealed class Handler : ICommandHandler<CreateProductCommand, Guid>
    {
        private readonly IProductRepository _productRepository;
        private readonly ISiteCategoryRepository _categoryRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IProductRepository productRepository,
            ISiteCategoryRepository categoryRepository,
            IAuditContext auditContext)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<Guid>.Failure("نام محصول الزامی است.");
            }

            if (request.Price is < 0)
            {
                return Result<Guid>.Failure("قیمت محصول نمی‌تواند منفی باشد.");
            }

            if (request.CompareAtPrice is < 0)
            {
                return Result<Guid>.Failure("قیمت قبل از تخفیف نامعتبر است.");
            }

            if (request.IsCustomOrder && request.Price.HasValue)
            {
                return Result<Guid>.Failure("محصولات سفارشی نمی‌توانند قیمت داشته باشند.");
            }

            if (!request.IsCustomOrder && !request.Price.HasValue)
            {
                return Result<Guid>.Failure("محصولات عادی باید قیمت داشته باشند.");
            }

            if (request.TrackInventory && request.StockQuantity < 0)
            {
                return Result<Guid>.Failure("موجودی محصول نمی‌تواند منفی باشد.");
            }

            if (request.Type == ProductType.Digital && string.IsNullOrWhiteSpace(request.DigitalDownloadPath))
            {
                return Result<Guid>.Failure("مسیر دانلود برای محصولات دانلودی الزامی است.");
            }

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category is null)
            {
                return Result<Guid>.Failure("دسته‌بندی انتخاب شده یافت نشد.");
            }

            if (category.Scope is not (CategoryScope.General or CategoryScope.Product))
            {
                return Result<Guid>.Failure("دسته‌بندی انتخاب شده برای محصولات مجاز نیست.");
            }

            // Generate slug with unique code prefix: code~slug
            var finalSlug = await GenerateUniqueSlugWithCodeAsync(
                string.IsNullOrWhiteSpace(request.SeoSlug) ? request.Name : request.SeoSlug,
                null,
                cancellationToken);

            var slugExists = await _productRepository.ExistsBySlugAsync(finalSlug, null, cancellationToken);
            if (slugExists)
            {
                return Result<Guid>.Failure("مسیر انتخاب شده قبلاً استفاده شده است.");
            }

            var gallery = request.Gallery?
                .Where(item => !string.IsNullOrWhiteSpace(item.Path))
                .Select(item => (item.Path.Trim(), item.Order))
                .ToList();

            var product = new Product(
                request.Name,
                request.Summary,
                request.Description,
                request.Type,
                request.Price ?? 0,
                request.CompareAtPrice,
                request.TrackInventory,
                request.StockQuantity,
                category,
                request.SeoTitle,
                request.SeoDescription,
                request.SeoKeywords,
                finalSlug,
                request.Robots,
                request.FeaturedImagePath,
                request.Tags ?? Array.Empty<string>(),
                request.DigitalDownloadPath,
                request.IsPublished,
                request.PublishedAt,
                gallery,
                request.SellerId,
                request.IsCustomOrder,
                request.Brand);

            if (request.IsCustomOrder)
            {
                product.SetCustomOrder(true);
            }

            var audit = _auditContext.Capture();

            product.CreatorId = audit.UserId;
            product.CreateDate = audit.Timestamp;
            product.UpdateDate = audit.Timestamp;
            product.Ip = audit.IpAddress;

            await _productRepository.AddAsync(product, cancellationToken);

            return Result<Guid>.Success(product.Id);
        }

        private async Task<string> GenerateUniqueSlugWithCodeAsync(string input, Guid? excludeProductId, CancellationToken cancellationToken)
        {
            // Extract existing code if slug already has code~slug format
            string? existingCode = null;
            string slugPart = input;
            
            if (input.Contains('~'))
            {
                var parts = input.Split('~', 2);
                if (parts.Length == 2 && parts[0].Length == 8)
                {
                    existingCode = parts[0];
                    slugPart = parts[1];
                }
            }
            
            // Generate unique 8-character code (use existing if provided)
            var uniqueCode = existingCode ?? GenerateUniqueCode();
            
            // Normalize slug from input
            var normalizedSlug = NormalizeSlug(slugPart);
            
            // Combine: code~slug
            var finalSlug = $"{uniqueCode}~{normalizedSlug}";
            
            // Ensure uniqueness
            var attempt = 1;
            while (await _productRepository.ExistsBySlugAsync(finalSlug, excludeProductId, cancellationToken))
            {
                if (attempt >= 100)
                {
                    // If too many attempts, generate new code
                    uniqueCode = GenerateUniqueCode();
                    finalSlug = $"{uniqueCode}~{normalizedSlug}";
                    attempt = 1;
                }
                else
                {
                    uniqueCode = GenerateUniqueCode();
                    finalSlug = $"{uniqueCode}~{normalizedSlug}";
                    attempt++;
                }
            }
            
            return finalSlug;
        }

        private static string GenerateUniqueCode()
        {
            // Generate 8-character hexadecimal code
            return Guid.NewGuid().ToString("N")[..8];
        }

        private static string NormalizeSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Guid.NewGuid().ToString("N")[..8];
            }

            var normalized = input.Trim().ToLowerInvariant();
            var builder = new StringBuilder(normalized.Length);
            var previousDash = false;

            foreach (var character in normalized)
            {
                if (char.IsLetterOrDigit(character) || (character >= '\u0600' && character <= '\u06FF'))
                {
                    builder.Append(character);
                    previousDash = false;
                }
                else if (char.IsWhiteSpace(character) || character is '-' or '_' or '\u200c')
                {
                    if (!previousDash)
                    {
                        builder.Append('-');
                        previousDash = true;
                    }
                }
            }

            var result = builder.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(result) ? Guid.NewGuid().ToString("N")[..8] : result;
        }
    }
}
