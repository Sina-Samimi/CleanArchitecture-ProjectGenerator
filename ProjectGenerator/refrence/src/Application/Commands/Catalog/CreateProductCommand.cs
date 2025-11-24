using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities.Catalog;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Commands.Catalog;

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
    string? TeacherId) : ICommand<Guid>
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

            if (request.Price < 0)
            {
                return Result<Guid>.Failure("قیمت محصول نمی‌تواند منفی باشد.");
            }

            if (request.CompareAtPrice is < 0)
            {
                return Result<Guid>.Failure("قیمت قبل از تخفیف نامعتبر است.");
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

            var slugExists = await _productRepository.ExistsBySlugAsync(request.SeoSlug, null, cancellationToken);
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
                request.Tags ?? Array.Empty<string>(),
                request.DigitalDownloadPath,
                request.IsPublished,
                request.PublishedAt,
                gallery,
                request.TeacherId);

            var audit = _auditContext.Capture();

            product.CreatorId = audit.UserId;
            product.CreateDate = audit.Timestamp;
            product.UpdateDate = audit.Timestamp;
            product.Ip = audit.IpAddress;

            await _productRepository.AddAsync(product, cancellationToken);

            return Result<Guid>.Success(product.Id);
        }
    }
}
