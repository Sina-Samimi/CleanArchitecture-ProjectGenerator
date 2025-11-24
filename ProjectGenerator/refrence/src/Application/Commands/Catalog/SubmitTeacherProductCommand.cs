using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Catalog;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Catalog;

public sealed record SubmitTeacherProductCommand(
    string Name,
    string? Summary,
    string Description,
    ProductType Type,
    decimal Price,
    bool TrackInventory,
    int StockQuantity,
    Guid CategoryId,
    string? Tags,
    string? FeaturedImagePath,
    string? DigitalDownloadPath) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<SubmitTeacherProductCommand, Guid>
    {
        private const string DefaultRobots = "noindex,nofollow";
        private const int MaxSlugAttempts = 200;
        private const int MaxSeoDescriptionLength = 180;

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

        public async Task<Result<Guid>> Handle(SubmitTeacherProductCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<Guid>.Failure("نام محصول الزامی است.");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return Result<Guid>.Failure("توضیحات محصول را وارد کنید.");
            }

            if (request.Price < 0)
            {
                return Result<Guid>.Failure("قیمت محصول نمی‌تواند منفی باشد.");
            }

            var trackInventory = request.Type == ProductType.Physical && request.TrackInventory;
            if (trackInventory && request.StockQuantity < 0)
            {
                return Result<Guid>.Failure("موجودی محصول نمی‌تواند منفی باشد.");
            }

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category is null)
            {
                return Result<Guid>.Failure("دسته‌بندی انتخاب شده یافت نشد.");
            }

            if (category.Scope is not (CategoryScope.General or CategoryScope.Product))
            {
                return Result<Guid>.Failure("انتخاب این دسته‌بندی برای محصولات مجاز نیست.");
            }

            var digitalPath = request.Type == ProductType.Digital
                ? request.DigitalDownloadPath?.Trim()
                : null;

            if (request.Type == ProductType.Digital && string.IsNullOrWhiteSpace(digitalPath))
            {
                return Result<Guid>.Failure("برای محصولات دانلودی وارد کردن لینک فایل الزامی است.");
            }

            if (digitalPath is { Length: > 600 })
            {
                return Result<Guid>.Failure("آدرس فایل دانلودی نمی‌تواند بیش از ۶۰۰ کاراکتر باشد.");
            }

            var featuredImagePath = string.IsNullOrWhiteSpace(request.FeaturedImagePath)
                ? null
                : request.FeaturedImagePath.Trim();

            if (featuredImagePath is { Length: > 600 })
            {
                return Result<Guid>.Failure("آدرس تصویر شاخص نمی‌تواند بیش از ۶۰۰ کاراکتر باشد.");
            }

            var slug = await GenerateUniqueSlugAsync(request.Name, cancellationToken);
            var tags = ParseTags(request.Tags);
            var seoKeywords = string.Join(", ", tags);
            var seoDescription = BuildSeoDescription(request.Summary, request.Description);

            var stockQuantity = trackInventory ? request.StockQuantity : 0;

            var audit = _auditContext.Capture();

            var product = new Product(
                request.Name.Trim(),
                request.Summary ?? string.Empty,
                request.Description.Trim(),
                request.Type,
                decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero),
                compareAtPrice: null,
                trackInventory,
                stockQuantity,
                category,
                seoTitle: request.Name.Trim(),
                seoDescription,
                seoKeywords,
                slug,
                DefaultRobots,
                featuredImagePath,
                tags,
                digitalPath,
                isPublished: false,
                publishedAt: null,
                gallery: null,
                teacherId: audit.UserId);

            product.CreatorId = audit.UserId;
            product.Ip = audit.IpAddress;
            product.CreateDate = audit.Timestamp;
            product.UpdateDate = audit.Timestamp;
            product.IsDeleted = false;

            await _productRepository.AddAsync(product, cancellationToken);

            return Result<Guid>.Success(product.Id);
        }

        private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken cancellationToken)
        {
            var baseSlug = GenerateSlugCandidate(name);
            var slug = baseSlug;
            var attempt = 1;

            while (await _productRepository.ExistsBySlugAsync(slug, null, cancellationToken))
            {
                if (attempt >= MaxSlugAttempts)
                {
                    return $"{baseSlug}-{Guid.NewGuid():N}";
                }

                slug = $"{baseSlug}-{attempt}";
                attempt++;
            }

            return slug;
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
            var candidate = !string.IsNullOrWhiteSpace(summary)
                ? summary.Trim()
                : description?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(candidate))
            {
                return string.Empty;
            }

            return candidate.Length <= MaxSeoDescriptionLength
                ? candidate
                : candidate[..MaxSeoDescriptionLength];
        }
    }
}
