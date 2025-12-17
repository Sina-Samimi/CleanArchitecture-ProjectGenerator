using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Abstractions.Messaging;
using LogsDtoCloneTest.Application.DTOs.Catalog;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Enums;
using LogsDtoCloneTest.SharedKernel.BaseTypes;

namespace LogsDtoCloneTest.Application.Queries.Catalog;

public sealed record GetSiteCategoriesQuery(CategoryScope Scope) : IQuery<IReadOnlyCollection<SiteCategoryDto>>
{
    public sealed class Handler : IQueryHandler<GetSiteCategoriesQuery, IReadOnlyCollection<SiteCategoryDto>>
    {
        private readonly ISiteCategoryRepository _categoryRepository;

        public Handler(ISiteCategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<IReadOnlyCollection<SiteCategoryDto>>> Handle(GetSiteCategoriesQuery request, CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(request.Scope))
            {
                return Result<IReadOnlyCollection<SiteCategoryDto>>.Failure("دامنه دسته‌بندی معتبر نیست.");
            }

            var categories = await _categoryRepository.GetTreeAsync(request.Scope, cancellationToken);
            var dto = categories
                .Where(category => category.ParentId is null && !category.IsDeleted)
                .Select(category => MapCategory(category, 0))
                .ToArray();

            return Result<IReadOnlyCollection<SiteCategoryDto>>.Success(dto);
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
