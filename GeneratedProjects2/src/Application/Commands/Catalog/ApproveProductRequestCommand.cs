using System;
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

public sealed record ApproveProductRequestCommand(
    Guid ProductRequestId,
    bool IsPublished = true,
    DateTimeOffset? PublishedAt = null) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<ApproveProductRequestCommand, Guid>
    {
        private const int MaxSlugAttempts = 200;

        private readonly IProductRequestRepository _productRequestRepository;
        private readonly IProductRepository _productRepository;
        private readonly IProductOfferRepository _productOfferRepository;
        private readonly ISiteCategoryRepository _categoryRepository;
        private readonly IAuditContext _auditContext;

        public Handler(
            IProductRequestRepository productRequestRepository,
            IProductRepository productRepository,
            IProductOfferRepository productOfferRepository,
            ISiteCategoryRepository categoryRepository,
            IAuditContext auditContext)
        {
            _productRequestRepository = productRequestRepository;
            _productRepository = productRepository;
            _productOfferRepository = productOfferRepository;
            _categoryRepository = categoryRepository;
            _auditContext = auditContext;
        }

    public async Task<Result<Guid>> Handle(ApproveProductRequestCommand request, CancellationToken cancellationToken)
    {
        var productRequest = await _productRequestRepository.GetByIdWithDetailsAsync(
            request.ProductRequestId, 
            cancellationToken);

        if (productRequest is null)
        {
            return Result<Guid>.Failure("درخواست محصول یافت نشد.");
        }

        if (productRequest.IsDeleted)
        {
            return Result<Guid>.Failure("درخواست محصول حذف شده است.");
        }

        if (productRequest.Status != ProductRequestStatus.Pending)
        {
            return Result<Guid>.Failure($"درخواست محصول در وضعیت '{GetStatusText(productRequest.Status)}' است و نمی‌توان آن را تایید کرد.");
        }

        var audit = _auditContext.Capture();
        if (string.IsNullOrWhiteSpace(audit.UserId))
        {
            return Result<Guid>.Failure("شناسه کاربری یافت نشد.");
        }

        Guid approvedId;

        // Determine request type: New Product vs Offer for Existing Product
        if (productRequest.IsNewProductRequest)
        {
            // Case 1: New Product Request
            var category = await _categoryRepository.GetByIdAsync(productRequest.CategoryId, cancellationToken);
            if (category is null)
            {
                return Result<Guid>.Failure("دسته‌بندی محصول یافت نشد.");
            }

            // Generate unique slug for approval
            var uniqueSlug = await GenerateUniqueSlugForApprovalAsync(
                productRequest.Name, 
                productRequest.SeoSlug, 
                cancellationToken);

            // Create Product from ProductRequest
            var galleryItems = productRequest.GetGalleryItems();
            var product = new Product(
                productRequest.Name,
                productRequest.Summary,
                productRequest.Description,
                productRequest.Type,
                productRequest.Price,
                compareAtPrice: null,
                productRequest.TrackInventory,
                productRequest.StockQuantity,
                category,
                productRequest.SeoTitle ?? productRequest.Name,
                productRequest.SeoDescription ?? string.Empty,
                productRequest.SeoKeywords ?? string.Empty,
                uniqueSlug,
                productRequest.Robots ?? "noindex,nofollow",
                productRequest.FeaturedImagePath,
                productRequest.GetTags(),
                productRequest.DigitalDownloadPath,
                request.IsPublished,
                request.PublishedAt ?? DateTimeOffset.UtcNow,
                galleryItems,
                productRequest.SellerId,
                productRequest.IsCustomOrder,
                productRequest.Brand);

            product.CreatorId = productRequest.CreatorId;
            product.Ip = productRequest.Ip;
            product.CreateDate = productRequest.CreateDate;
            product.UpdateDate = DateTimeOffset.UtcNow;
            product.IsDeleted = false;

            await _productRepository.AddAsync(product, cancellationToken);
            approvedId = product.Id;
        }
        else
        {
            // Case 2: Offer for Existing Product
            if (!productRequest.TargetProductId.HasValue)
            {
                return Result<Guid>.Failure("شناسه محصول هدف مشخص نشده است.");
            }

            var targetProduct = await _productRepository.GetByIdAsync(
                productRequest.TargetProductId.Value, 
                cancellationToken);

            if (targetProduct is null || targetProduct.IsDeleted)
            {
                return Result<Guid>.Failure("محصول مورد نظر یافت نشد.");
            }

            // Check if offer already exists for this seller and product
            var existingOffer = await _productOfferRepository.GetByProductIdAndSellerIdAsync(
                productRequest.TargetProductId.Value,
                productRequest.SellerId,
                cancellationToken);

            if (existingOffer is not null && !existingOffer.IsDeleted)
            {
                return Result<Guid>.Failure("شما قبلاً برای این محصول پیشنهاد ثبت کرده‌اید.");
            }

            // Create ProductOffer from ProductRequest
            var offer = new ProductOffer(
                productRequest.TargetProductId.Value,
                productRequest.SellerId,
                productRequest.Price,
                productRequest.TrackInventory,
                productRequest.StockQuantity,
                compareAtPrice: null,
                isActive: true,
                isPublished: request.IsPublished,
                approvedFromRequestId: productRequest.Id);

            offer.CreatorId = productRequest.CreatorId;
            offer.Ip = productRequest.Ip;
            offer.CreateDate = productRequest.CreateDate;
            offer.UpdateDate = DateTimeOffset.UtcNow;
            offer.IsDeleted = false;

            if (request.IsPublished)
            {
                offer.Publish(request.PublishedAt ?? DateTimeOffset.UtcNow);
            }

            await _productOfferRepository.AddAsync(offer, cancellationToken);
            approvedId = offer.Id;
        }

        // Approve the request and link it to the created product/offer
        var approvedProductId = productRequest.IsNewProductRequest 
            ? approvedId 
            : productRequest.TargetProductId!.Value;

        productRequest.Approve(audit.UserId, approvedProductId);
        productRequest.UpdaterId = audit.UserId;
        productRequest.UpdateDate = DateTimeOffset.UtcNow;

        await _productRequestRepository.UpdateAsync(productRequest, cancellationToken);

        return Result<Guid>.Success(approvedId);
    }

        private async Task<string> GenerateUniqueSlugForApprovalAsync(
            string productName, 
            string originalSlug, 
            CancellationToken cancellationToken)
        {
            // Try to use a slug based on the product name first
            var baseSlug = GenerateSlugCandidate(productName);
            var slug = baseSlug;
            var attempt = 1;

            // Check both Products and ProductRequests for unique slug
            while (await SlugExistsAsync(slug, cancellationToken))
            {
                if (attempt >= MaxSlugAttempts)
                {
                    // If we've exhausted attempts, use original slug with GUID to guarantee uniqueness
                    var guidPart = Guid.NewGuid().ToString("N")[..8];
                    return $"{baseSlug}-{guidPart}";
                }

                slug = $"{baseSlug}-{attempt}";
                attempt++;
            }

            return slug;
        }

        private async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
        {
            // Check in Products table
            if (await _productRepository.ExistsBySlugAsync(slug, null, cancellationToken))
            {
                return true;
            }

            // Check in ProductRequests table (excluding the one being approved if needed)
            // We check all to ensure complete uniqueness
            if (await _productRequestRepository.ExistsBySlugAsync(slug, null, cancellationToken))
            {
                return true;
            }

            return false;
        }

        private static string GenerateSlugCandidate(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Guid.NewGuid().ToString("N")[..8];
            }

            var normalized = input.Trim().ToLowerInvariant();
            var builder = new System.Text.StringBuilder(normalized.Length);
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

        private static string GetStatusText(ProductRequestStatus status)
        {
            return status switch
            {
                ProductRequestStatus.Pending => "در انتظار بررسی",
                ProductRequestStatus.Approved => "تایید شده",
                ProductRequestStatus.Rejected => "رد شده",
                _ => "نامشخص"
            };
        }
    }
}

