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

public sealed record UpdateProductCommand(
    Guid Id,
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
    IReadOnlyCollection<UpdateProductCommand.ProductGalleryItem>? Gallery,
    string? TeacherId) : ICommand
{
    public sealed record ProductGalleryItem(string Path, int Order);

    public sealed class Handler : ICommandHandler<UpdateProductCommand>
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

        public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Failure("نام محصول الزامی است.");
            }

            if (request.Price < 0)
            {
                return Result.Failure("قیمت محصول نمی‌تواند منفی باشد.");
            }

            if (request.CompareAtPrice is < 0)
            {
                return Result.Failure("قیمت قبل از تخفیف نامعتبر است.");
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

            var slugExists = await _productRepository.ExistsBySlugAsync(request.SeoSlug, request.Id, cancellationToken);
            if (slugExists)
            {
                return Result.Failure("مسیر انتخاب شده قبلاً استفاده شده است.");
            }

            product.UpdateContent(request.Name, request.Summary, request.Description);
            product.UpdatePricing(request.Price, request.CompareAtPrice);
            product.ChangeType(request.Type, request.DigitalDownloadPath);
            product.UpdateInventory(request.TrackInventory, request.StockQuantity);
            product.SetCategory(category);
            product.SetFeaturedImage(request.FeaturedImagePath);
            product.SetTags(request.Tags);
            product.UpdateSeoMetadata(request.SeoTitle, request.SeoDescription, request.SeoKeywords, request.SeoSlug, request.Robots);
            product.AssignTeacher(request.TeacherId);

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

            return Result.Success();
        }
    }
}
