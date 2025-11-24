using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Tests;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Tests;

public sealed record GetTestListQuery(
    TestType? Type,
    TestStatus? Status,
    string? SearchTerm,
    int Page,
    int PageSize) : IQuery<PagedTestListDto>
{
    public sealed class Handler : IQueryHandler<GetTestListQuery, PagedTestListDto>
    {
        private readonly ITestRepository _testRepository;

        public Handler(ITestRepository testRepository)
        {
            _testRepository = testRepository;
        }

        public async Task<Result<PagedTestListDto>> Handle(GetTestListQuery request, CancellationToken cancellationToken)
        {
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : Math.Clamp(request.PageSize, 5, 100);

            var (tests, totalCount) = await _testRepository.GetPagedAsync(
                page,
                pageSize,
                request.Type,
                request.Status,
                request.SearchTerm,
                cancellationToken);

            var items = tests.Select(t => new TestListDto
            {
                Id = t.Id,
                Title = t.Title,
                Type = t.Type,
                Status = t.Status,
                CategoryId = t.CategoryId,
                CategoryName = t.Category?.Name,
                Price = t.Price,
                Currency = t.Currency ?? "IRT",
                QuestionsCount = t.Questions.Count,
                AttemptsCount = t.Attempts.Count,
                CreateDate = t.CreateDate
            }).ToList();

            var result = new PagedTestListDto
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Result<PagedTestListDto>.Success(result);
        }
    }
}

public sealed record PagedTestListDto
{
    public List<TestListDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
