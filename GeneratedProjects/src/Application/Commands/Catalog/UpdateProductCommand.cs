using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Application.Services;
using TestAttarClone.Domain.Entities.Catalog;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Catalog;

public sealed record UpdateProductCommand(
    Guid Id,
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
    IReadOnlyCollection<UpdateProductCommand.ProductGalleryItem>? Gallery,
    string? SellerId,
    bool IsCustomOrder = false,
    string? Brand = null) : ICommand
{
    public sealed record ProductGalleryItem(string Path, int Order);

    public sealed class Handler : ICommandHandler<UpdateProductCommand>
    {
        private const int MaxSlugAttempts = 200;

        private readonly IProductRepository _productRepository;
        private readonly IProductRequestRepository _productRequestRepository;
        private readonly ISiteCategoryRepository _categoryRepository;
        private readonly IAuditContext _auditContext;
        private readonly IBackInStockNotificationService _backInStockNotificationService;

        public Handler(
            IProductRepository productRepository,
            IProductRequestRepository productRequestRepository,
            ISiteCategoryRepository categoryRepository,
            IAuditContext auditContext,
            IBackInStockNotificationService backInStockNotificationService)
        {
            _productRepository = productRepository;
            _productRequestRepository = productRequestRepository;
            _categoryRepository = categoryRepository;
            _auditContext = auditContext;
            _backInStockNotificationService = backInStockNotificationService;
        }

        public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure("نام محصول الزامی است.");
            }

            if (request.Price is < 0)
            {
                return Result.Failure("قیمت محصول نمی‌تواند منفی باشد.");
            }

            if (request.CompareAtPrice is < 0)
            {
                return Result.Failure("قیمت قبل از تخفیف نامعتبر است.");
            }

            if (request.IsCustomOrder && request.Price.HasValue)
            {
                return Result.Failure("محصولات سفارشی نمی‌توانند قیمت داشته باشند.");
            }

            if (!request.IsCustomOrder && !request.Price.HasValue)
            {
                return Result.Failure("محصولات عادی باید قیمت داشته باشند.");
            }

            if (request.TrackInventory && request.StockQuantity < 0)
            {
                return Result.Failure("موجودی محصول نمی‌تواند منفی باشد.");
            }

            if (request.Type == ProductType.Digital && string.IsNullOrWhiteSpace(request.DigitalDownloadPath))
            {
                return Result.Failure("مسیر دانلود برای محصولات دانلودی الزامی است.");
            }

            var product = await _productRepository.GetWithDetailsAsync(request.Id, cancellationToken);
            if (product is null)
            {
                return Result.Failure("محصول مورد نظر یافت نشد.");
            }

            SiteCategory category;
            if (product.CategoryId == request.CategoryId && product.Category is not null)
            {
                category = product.Category;
            }
            else
            {
                var categoryResult = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                if (categoryResult is null)
                {
                    return Result.Failure("دسته‌بندی انتخاب شده یافت نشد.");
                }

                category = categoryResult;
            }

            if (category.Scope is not (CategoryScope.General or CategoryScope.Product))
            {
                return Result.Failure("دسته‌بندی انتخاب شده برای محصولات مجاز نیست.");
            }

            // Generate slug with unique code prefix: code~slug
            // Extract existing code from current product slug to preserve it
            string? existingProductCode = null;
            if (!string.IsNullOrWhiteSpace(product.SeoSlug) && product.SeoSlug.Contains('~'))
            {
                var parts = product.SeoSlug.Split('~', 2);
                if (parts.Length == 2 && parts[0].Length == 8)
                {
                    existingProductCode = parts[0];
                }
            }
            
            // Use provided slug or generate from product name
            var slugInput = string.IsNullOrWhiteSpace(request.SeoSlug) ? request.Name.Trim() : request.SeoSlug;
            var slug = await GenerateUniqueSlugWithCodeAsync(slugInput, request.Id, existingProductCode, cancellationToken);

            product.UpdateContent(request.Name.Trim(), request.Summary, request.Description);
            product.SetCustomOrder(request.IsCustomOrder);
            product.UpdatePricing(request.Price, request.CompareAtPrice);
            product.ChangeType(request.Type, request.DigitalDownloadPath);

            var previousTrackInventory = product.TrackInventory;
            var previousStockQuantity = product.StockQuantity;

            product.UpdateInventory(request.TrackInventory, request.StockQuantity);
            product.SetCategory(category);
            product.SetFeaturedImage(request.FeaturedImagePath);
            product.SetTags(request.Tags);
            product.UpdateSeoMetadata(request.SeoTitle, request.SeoDescription, request.SeoKeywords, slug, request.Robots);
            product.AssignSeller(request.SellerId);
            product.SetBrand(request.Brand);

            if (request.IsPublished)
            {
                product.Publish(request.PublishedAt);
            }
            else
            {
                product.Unpublish();
            }

            var gallery = request.Gallery?
                .Where(item => !string.IsNullOrWhiteSpace(item.Path))
                .Select(item => (item.Path.Trim(), item.Order))
                .ToList();
            product.ReplaceGallery(gallery);

            var audit = _auditContext.Capture();

            product.UpdaterId = audit.UserId;
            product.UpdateDate = audit.Timestamp;
            product.Ip = audit.IpAddress;

            await _productRepository.UpdateAsync(product, cancellationToken);

            // اگر موجودی از ناموجود به موجود تغییر کرد، نوتیفیکیشن‌ها را ارسال کن
            if (request.TrackInventory &&
                (!previousTrackInventory || previousStockQuantity <= 0) &&
                request.StockQuantity > 0)
            {
                await _backInStockNotificationService.NotifyProductBackInStockAsync(product.Id, product.Name, cancellationToken);
            }

            return Result.Success();
        }

        private async Task<string> GenerateUniqueSlugWithCodeAsync(string input, Guid? excludeProductId, string? preferredCode, CancellationToken cancellationToken)
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
            
            // Use preferred code (from existing product), existing code (from input), or generate new
            var uniqueCode = preferredCode ?? existingCode ?? GenerateUniqueCode();
            
            // Normalize slug from input
            var normalizedSlug = NormalizeSlug(slugPart);
            
            // Combine: code~slug
            var finalSlug = $"{uniqueCode}~{normalizedSlug}";
            
            // Ensure uniqueness
            var attempt = 1;
            while (await SlugExistsAsync(finalSlug, excludeProductId, cancellationToken))
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

        private async Task<bool> SlugExistsAsync(string slug, Guid? excludeProductId, CancellationToken cancellationToken)
        {
            // Check in Products table
            if (await _productRepository.ExistsBySlugAsync(slug, excludeProductId, cancellationToken))
            {
                return true;
            }

            // Check in ProductRequests table to ensure complete uniqueness
            if (await _productRequestRepository.ExistsBySlugAsync(slug, null, cancellationToken))
            {
                return true;
            }

            return false;
        }

    }
}
