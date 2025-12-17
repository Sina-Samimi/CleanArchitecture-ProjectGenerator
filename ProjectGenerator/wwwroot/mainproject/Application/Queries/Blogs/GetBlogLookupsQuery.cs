using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Abstractions.Messaging;
using Attar.Application.DTOs.Blogs;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Blogs;
using Attar.SharedKernel.BaseTypes;

namespace Attar.Application.Queries.Blogs;

public sealed record GetBlogLookupsQuery() : IQuery<BlogLookupsDto>
{
    public sealed class Handler : IQueryHandler<GetBlogLookupsQuery, BlogLookupsDto>
    {
        private readonly IBlogCategoryRepository _categoryRepository;
        private readonly IBlogAuthorRepository _authorRepository;

        public Handler(IBlogCategoryRepository categoryRepository, IBlogAuthorRepository authorRepository)
        {
            _categoryRepository = categoryRepository;
            _authorRepository = authorRepository;
        }

        public async Task<Result<BlogLookupsDto>> Handle(GetBlogLookupsQuery request, CancellationToken cancellationToken)
        {
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            var authors = await _authorRepository.GetAllAsync(cancellationToken);

            var categoryTree = BuildCategoryTree(categories);
            var authorDtos = authors
                .OrderBy(author => author.DisplayName, StringComparer.CurrentCulture)
                .Select(author => new BlogAuthorDto(author.Id, author.DisplayName, author.IsActive))
                .ToArray();

            return Result<BlogLookupsDto>.Success(new BlogLookupsDto(categoryTree, authorDtos));
        }

        private static IReadOnlyCollection<BlogCategoryDto> BuildCategoryTree(IReadOnlyCollection<BlogCategory> categories)
        {
            if (categories.Count == 0)
            {
                return Array.Empty<BlogCategoryDto>();
            }

            var grouped = categories
                .GroupBy(category => category.ParentId);

            var lookup = new Dictionary<Guid, List<BlogCategory>>();
            List<BlogCategory>? roots = null;

            foreach (var group in grouped)
            {
                var ordered = group
                    .OrderBy(category => category.Name, StringComparer.CurrentCulture)
                    .ToList();

                if (group.Key is Guid parentId)
                {
                    lookup[parentId] = ordered;
                }
                else
                {
                    roots = ordered;
                }
            }

            if (roots is null)
            {
                return Array.Empty<BlogCategoryDto>();
            }

            var result = new List<BlogCategoryDto>(roots.Count);
            foreach (var root in roots)
            {
                result.Add(CreateCategoryDto(root, lookup, 0));
            }

            return result;
        }

        private static BlogCategoryDto CreateCategoryDto(
            BlogCategory category,
            IReadOnlyDictionary<Guid, List<BlogCategory>> lookup,
            int depth)
        {
            var children = lookup.TryGetValue(category.Id, out var items)
                ? items.Select(child => CreateCategoryDto(child, lookup, depth + 1)).ToArray()
                : Array.Empty<BlogCategoryDto>();

            return new BlogCategoryDto(
                category.Id,
                category.Name,
                category.ParentId,
                category.Slug,
                category.Description,
                depth,
                children);
        }
    }
}
