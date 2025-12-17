using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Abstractions.Messaging;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Catalog;
using TestAttarClone.SharedKernel.BaseTypes;

namespace TestAttarClone.Application.Commands.Catalog;

public sealed record SubmitSellerProductOfferCommand(
    Guid ProductId,
    decimal? Price,
    bool TrackInventory,
    int StockQuantity,
    string? DigitalDownloadPath) : ICommand<Guid>
{
    public sealed class Handler : ICommandHandler<SubmitSellerProductOfferCommand, Guid>
    {
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

        public async Task<Result<Guid>> Handle(SubmitSellerProductOfferCommand request, CancellationToken cancellationToken)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Result<Guid>.Failure("شناسه محصول معتبر نیست.");
            }

            if (request.Price.HasValue && request.Price.Value < 0)
            {
                return Result<Guid>.Failure("قیمت محصول نمی‌تواند منفی باشد.");
            }

            var trackInventory = request.TrackInventory;
            if (trackInventory && request.StockQuantity < 0)
            {
                return Result<Guid>.Failure("موجودی محصول نمی‌تواند منفی باشد.");
            }

            // Verify product exists
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null || product.IsDeleted)
            {
                return Result<Guid>.Failure("محصول مورد نظر یافت نشد.");
            }

            // Check if offer already exists
            var audit = _auditContext.Capture();
            if (string.IsNullOrWhiteSpace(audit.UserId))
            {
                return Result<Guid>.Failure("شناسه کاربری یافت نشد.");
            }

            var existingOffer = await _productOfferRepository.GetByProductIdAndSellerIdAsync(
                request.ProductId,
                audit.UserId,
                cancellationToken);

            if (existingOffer is not null && !existingOffer.IsDeleted)
            {
                return Result<Guid>.Failure("شما قبلاً برای این محصول پیشنهاد ثبت کرده‌اید.");
            }

            // Check for pending request for the same product
            var sellerRequests = await _productRequestRepository.GetBySellerIdAsync(audit.UserId, cancellationToken);
            var pendingOfferRequest = sellerRequests
                .FirstOrDefault(r => r.TargetProductId == request.ProductId 
                    && r.Status == Domain.Enums.ProductRequestStatus.Pending 
                    && !r.IsDeleted);

            if (pendingOfferRequest is not null)
            {
                return Result<Guid>.Failure("شما قبلاً درخواستی برای این محصول ثبت کرده‌اید که در انتظار تایید است.");
            }

            // Get category - product.Category may be null because it's not included
            var category = await _categoryRepository.GetByIdAsync(product.CategoryId, cancellationToken);
            if (category is null || category.IsDeleted)
            {
                return Result<Guid>.Failure("دسته‌بندی محصول یافت نشد.");
            }

            // Validate digital download path if product is digital
            var digitalPath = product.Type == Domain.Enums.ProductType.Digital
                ? (!string.IsNullOrWhiteSpace(request.DigitalDownloadPath) 
                    ? request.DigitalDownloadPath.Trim() 
                    : product.DigitalDownloadPath)
                : null;

            if (product.Type == Domain.Enums.ProductType.Digital && string.IsNullOrWhiteSpace(digitalPath))
            {
                return Result<Guid>.Failure("برای محصولات دانلودی وارد کردن لینک فایل الزامی است.");
            }

            if (digitalPath is { Length: > 600 })
            {
                return Result<Guid>.Failure("آدرس فایل دانلودی نمی‌تواند بیش از ۶۰۰ کاراکتر باشد.");
            }

            // Create ProductRequest for offer (minimal info, mostly from existing product)
            var stockQuantity = trackInventory ? request.StockQuantity : 0;
            var slug = await GenerateOfferSlugAsync(product, audit.UserId, cancellationToken);

            var productRequest = new ProductRequest(
                product.Name, // Use product name
                product.Summary ?? string.Empty,
                product.Description,
                product.Type,
                request.Price,
                trackInventory,
                stockQuantity,
                category,
                product.FeaturedImagePath,
                product.Tags,
                digitalPath,
                audit.UserId,
                seoTitle: product.SeoTitle ?? product.Name,
                seoDescription: product.SeoDescription ?? string.Empty,
                seoKeywords: product.SeoKeywords ?? string.Empty,
                slug,
                product.Robots ?? "noindex,nofollow",
                isCustomOrder: !request.Price.HasValue,
                gallery: null,
                targetProductId: request.ProductId); // This marks it as an offer request

            productRequest.CreatorId = audit.UserId;
            productRequest.Ip = audit.IpAddress;
            productRequest.CreateDate = audit.Timestamp;
            productRequest.UpdateDate = audit.Timestamp;
            productRequest.IsDeleted = false;

            await _productRequestRepository.AddAsync(productRequest, cancellationToken);

            return Result<Guid>.Success(productRequest.Id);
        }

        private async Task<string> GenerateOfferSlugAsync(
            Domain.Entities.Catalog.Product product, 
            string sellerId, 
            CancellationToken cancellationToken)
        {
            // Generate a unique slug for the offer request
            // Format: product-slug-offer-{sellerId-hash}
            var baseSlug = product.SeoSlug;
            var sellerHash = sellerId.GetHashCode().ToString("X8");
            var slug = $"{baseSlug}-offer-{sellerHash}";

            // Ensure uniqueness
            var attempt = 1;
            while (await _productRequestRepository.ExistsBySlugAsync(slug, null, cancellationToken))
            {
                slug = $"{baseSlug}-offer-{sellerHash}-{attempt}";
                attempt++;

                if (attempt > 100)
                {
                    slug = $"{baseSlug}-offer-{Guid.NewGuid():N}";
                    break;
                }
            }

            return slug;
        }
    }
}

