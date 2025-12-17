using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Abstractions.Messaging;
using LogTableRenameTest.Application.DTOs.Catalog;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Enums;
using LogTableRenameTest.SharedKernel.BaseTypes;

namespace LogTableRenameTest.Application.Queries.Catalog;

public sealed record GetProductLookupsQuery : IQuery<ProductLookupsDto>
{
    public sealed class Handler : IQueryHandler<GetProductLookupsQuery, ProductLookupsDto>
    {
        private readonly ISiteCategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;

        public Handler(ISiteCategoryRepository categoryRepository, IProductRepository productRepository)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
        }

        public async Task<Result<ProductLookupsDto>> Handle(GetProductLookupsQuery request, CancellationToken cancellationToken)
        {
            var categories = await _categoryRepository.GetTreeAsync(CategoryScope.Product, cancellationToken);
            var categoryDtos = categories
                .Where(category => category.ParentId is null)
                .Select(category => MapCategory(category, 0))
                .ToArray();

            var tags = await _productRepository.GetAllTagsAsync(cancellationToken);

            return Result<ProductLookupsDto>.Success(new ProductLookupsDto(categoryDtos, tags));
        }

        private static SiteCategoryDto MapCategory(Domain.Entities.Catalog.SiteCategory category, int depth)
        {
            var children = category.Children
                .Where(child => !child.IsDeleted)
                .OrderBy(child => child.Name)
                .Select(child => MapCategory(child, depth + 1))
                .ToArray();

            return new SiteCategoryDto(
                category.Id,
                category.Name,
                category.Slug,
                category.Description,
                category.ImageUrl,
                category.Scope,
                category.ParentId,
                depth,
                children);
        }
    }
}
