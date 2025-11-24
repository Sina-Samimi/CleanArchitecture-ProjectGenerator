using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Tests;
using Arsis.Application.Interfaces;
using Arsis.Domain.Enums;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Tests;

public sealed record GetPublicTestListQuery(
    string? Search,
    TestType? Type,
    Guid? CategoryId,
    bool? IsFree,
    int Page = 1,
    int PageSize = 12
) : IQuery<PagedTestListResult>;

public sealed record PagedTestListResult
{
    public required System.Collections.Generic.List<TestListDto> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}

public sealed class GetPublicTestListQueryHandler : IQueryHandler<GetPublicTestListQuery, PagedTestListResult>
{
    private readonly ITestRepository _testRepository;

    public GetPublicTestListQueryHandler(ITestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    public async Task<Result<PagedTestListResult>> Handle(GetPublicTestListQuery request, CancellationToken cancellationToken)
    {
        var tests = await _testRepository.GetPublishedAsync(cancellationToken);

        // Apply filters
        var now = DateTimeOffset.UtcNow;
        var filtered = tests.Where(t => t.IsAvailable(now)).ToList();

        // Always include CliftonSchwartz tests if they are available (regardless of Status)
        // This allows custom CliftonSchwartz tests to be displayed even if not explicitly Published
        var cliftonSchwartzTests = await _testRepository.GetByTypeAsync(TestType.CliftonSchwartz, cancellationToken);
        foreach (var special in cliftonSchwartzTests)
        {
            // For CliftonSchwartz tests, check availability manually (without Status check)
            var isAvailable = true;
            if (special.AvailableFrom.HasValue && now < special.AvailableFrom.Value)
            {
                isAvailable = false;
            }
            if (special.AvailableUntil.HasValue && now > special.AvailableUntil.Value)
            {
                isAvailable = false;
            }
            
            if (isAvailable && filtered.All(existing => existing.Id != special.Id))
            {
                filtered.Add(special);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            filtered = filtered.Where(t =>
                t.Title.ToLower().Contains(searchLower) ||
                (t.Description != null && t.Description.ToLower().Contains(searchLower)))
                .ToList();
        }

        if (request.Type.HasValue)
        {
            filtered = filtered.Where(t => t.Type == request.Type.Value).ToList();
        }

        if (request.CategoryId.HasValue)
        {
            filtered = filtered.Where(t => t.CategoryId == request.CategoryId.Value).ToList();
        }

        if (request.IsFree.HasValue)
        {
            if (request.IsFree.Value)
            {
                filtered = filtered.Where(t => t.Price == 0).ToList();
            }
            else
            {
                filtered = filtered.Where(t => t.Price > 0).ToList();
            }
        }

        var totalCount = filtered.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var page = Math.Max(1, Math.Min(request.Page, Math.Max(1, totalPages)));

        var ordered = filtered
            .OrderByDescending(t => t.Type == TestType.CliftonSchwartz)
            .ThenByDescending(t => t.CreateDate)
            .ToList();

        var items = ordered
            .Skip((page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TestListDto
            {
                Id = t.Id,
                Title = t.Title,
                Type = t.Type,
                Status = t.Status,
                Price = t.Price,
                Currency = t.Currency ?? "IRT",
                QuestionsCount = t.Questions.Count,
                AttemptsCount = t.Attempts.Count,
                CreateDate = t.CreateDate,
                CategoryName = t.Category != null ? t.Category.Name : null
            })
            .ToList();

        var result = new PagedTestListResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };

        return Result<PagedTestListResult>.Success(result);
    }
}
