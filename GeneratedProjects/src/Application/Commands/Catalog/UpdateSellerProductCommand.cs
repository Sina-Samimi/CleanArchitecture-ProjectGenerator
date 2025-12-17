using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Enums;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Catalog;

public sealed record UpdateSellerProductCommand(
    Guid ProductId,
    string SellerId,
    string Name,
    string? Summary,
    string Description,
    ProductType Type,
    decimal? Price,
    bool TrackInventory,
    int StockQuantity,
    Guid CategoryId,
    string? Tags,
    string? FeaturedImagePath,
    string? DigitalDownloadPath,
    string? Brand) : ICommand<bool>
{
    public sealed class Handler : ICommandHandler<UpdateSellerProductCommand, bool>
    {
        private const string DefaultRobots = "noindex,nofollow";
        private const int MaxSlugAttempts = 200;
        private const int MaxSeoDescriptionLength = 180;

        private readonly IProductRepository _productRepository;
        private readonly IProductRequestRepository _productRequestRepository;
        private readonly ISiteCategoryRepository _categoryRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IProductRepository productRepository,
            IProductRequestRepository productRequestRepository,
            ISiteCategoryRepository categoryRepository,
            IAuditContext auditContext)
        {
            _productRepository = productRepository;
            _productRequestRepository = productRequestRepository;
            _categoryRepository = categoryRepository;
            _auditContext = auditContext;
        }

        public async Task<Result<bool>> Handle(UpdateSellerProductCommand request, CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result<bool>.Failure("شناسه محصول معتبر نیست.");
            }

            if (string.IsNullOrWhiteSpace(request.SellerId))
            {
                return Result<bool>.Failure("دسترسی به محصول امکان‌پذیر نیست.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<bool>.Failure("نام محصول الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return Result<bool>.Failure("توضیحات محصول را وارد کنید.");
            }

            if (request.Price.HasValue && request.Price.Value < 0)
            {
                return Result<bool>.Failure("قیمت محصول نمی‌تواند منفی باشد.");
            }

            var trackInventory = request.Type == ProductType.Physical && request.TrackInventory;
            if (trackInventory && request.StockQuantity < 0)
            {
                return Result<bool>.Failure("موجودی محصول نمی‌تواند منفی باشد.");
            }

            var product = await _productRepository.GetWithDetailsAsync(request.ProductId, cancellationToken);
            if (product is null || product.IsDeleted)
            {
                return Result<bool>.Failure("محصول مورد نظر یافت نشد.");
            }

            var isOwner = string.Equals(product.CreatorId, request.SellerId, StringComparison.Ordinal)
                || string.Equals(product.SellerId, request.SellerId, StringComparison.Ordinal);

            if (!isOwner)
            {
                return Result<bool>.Failure("شما اجازه ویرایش این محصول را ندارید.");
            }

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category is null)
            {
                return Result<bool>.Failure("دسته‌بندی انتخاب شده یافت نشد.");
            }

            if (category.Scope is not (CategoryScope.General or CategoryScope.Product))
            {
                return Result<bool>.Failure("انتخاب این دسته‌بندی برای محصولات مجاز نیست.");
            }

            var digitalPath = request.Type == ProductType.Digital
                ? request.DigitalDownloadPath?.Trim()
                : null;

            if (request.Type == ProductType.Digital && string.IsNullOrWhiteSpace(digitalPath))
            {
                return Result<bool>.Failure("برای محصولات دانلودی وارد کردن لینک فایل الزامی است.");
            }

            if (digitalPath is { Length: > 600 })
            {
                return Result<bool>.Failure("آدرس فایل دانلودی نمی‌تواند بیش از ۶۰۰ کاراکتر باشد.");
            }

            var featuredImagePath = string.IsNullOrWhiteSpace(request.FeaturedImagePath)
                ? null
                : request.FeaturedImagePath.Trim();

            if (featuredImagePath is { Length: > 600 })
            {
                return Result<bool>.Failure("آدرس تصویر شاخص نمی‌تواند بیش از ۶۰۰ کاراکتر باشد.");
            }

            var normalizedName = request.Name.Trim();
            var tags = ParseTags(request.Tags);
            var seoKeywords = string.Join(", ", tags);
            var seoDescription = BuildSeoDescription(request.Summary, request.Description);
            var stockQuantity = trackInventory ? request.StockQuantity : 0;

            // Always generate slug based on product name to ensure uniqueness
            // This prevents duplicate slugs and ensures slug always reflects the current product name
            var slug = await GenerateUniqueSlugAsync(normalizedName, product.Id, cancellationToken);

            var wasPublished = product.IsPublished;

            product.UpdateContent(normalizedName, request.Summary ?? string.Empty, request.Description.Trim());
            product.ChangeType(request.Type, digitalPath);
            product.SetCustomOrder(!request.Price.HasValue);
            product.UpdatePricing(request.Price, compareAtPrice: null);
            product.UpdateInventory(trackInventory, stockQuantity);
            if (product.CategoryId != category.Id)
            {
                product.SetCategory(category);
            }
            product.SetTags(tags);
            product.SetFeaturedImage(featuredImagePath);
            product.SetBrand(request.Brand);
            product.UpdateSeoMetadata(normalizedName, seoDescription, seoKeywords, slug, DefaultRobots);
            product.AssignSeller(request.SellerId);
            product.Unpublish();

            var audit = _auditContext.Capture();
            product.UpdaterId = audit.UserId;
            product.UpdateDate = audit.Timestamp;
            product.Ip = audit.IpAddress;

            await _productRepository.UpdateAsync(product, cancellationToken);

            return Result<bool>.Success(wasPublished);
        }

        private async Task<string> GenerateUniqueSlugAsync(string name, Guid productId, CancellationToken cancellationToken)
        {
            var baseSlug = GenerateSlugCandidate(name);
            var slug = baseSlug;
            var attempt = 1;

            while (await SlugExistsAsync(slug, productId, cancellationToken))
            {
                if (attempt >= MaxSlugAttempts)
                {
                    var guidPart = Guid.NewGuid().ToString("N")[..8];
                    return $"{baseSlug}-{guidPart}";
                }

                slug = $"{baseSlug}-{attempt}";
                attempt++;
            }

            return slug;
        }

        private async Task<bool> SlugExistsAsync(string slug, Guid? excludeProductId, CancellationToken cancellationToken)
        {
            // Check in Products table
            if (await _productRepository.ExistsBySlugAsync(slug, excludeProductId, cancellationToken))
            {
                return true;
            }

            // Check in ProductRequests table to ensure complete uniqueness
            // Exclude product requests for the same seller's products if needed
            if (await _productRequestRepository.ExistsBySlugAsync(slug, null, cancellationToken))
            {
                return true;
            }

            return false;
        }

        private static string GenerateSlugCandidate(string input)
        {
            var normalized = input.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return Guid.NewGuid().ToString("N");
            }

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
            return string.IsNullOrWhiteSpace(result) ? Guid.NewGuid().ToString("N") : result;
        }

        private static IReadOnlyCollection<string> ParseTags(string? tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return Array.Empty<string>();
            }

            var separators = new[] { ',', '،', ';', '|', '\n', '\r' };

            return tags
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Length > 50 ? tag[..50] : tag)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string BuildSeoDescription(string? summary, string description)
        {
            var source = string.IsNullOrWhiteSpace(summary)
                ? description
                : string.Concat(summary.Trim(), " | ", description);

            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            var trimmed = source.Trim();
            return trimmed.Length <= MaxSeoDescriptionLength
                ? trimmed
                : trimmed[..MaxSeoDescriptionLength];
        }
    }
}
