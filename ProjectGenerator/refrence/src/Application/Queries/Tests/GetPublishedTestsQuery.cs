using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Tests;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Tests;

public sealed record GetPublishedTestsQuery : IQuery<List<TestListDto>>
{
    public sealed class Handler : IQueryHandler<GetPublishedTestsQuery, List<TestListDto>>
    {
        private readonly ITestRepository _testRepository;

        public Handler(ITestRepository testRepository)
        {
            _testRepository = testRepository;
        }

        public async Task<Result<List<TestListDto>>> Handle(GetPublishedTestsQuery request, CancellationToken cancellationToken)
        {
            var tests = await _testRepository.GetPublishedAsync(cancellationToken);

            var dtos = tests
                .Where(t => t.IsAvailable(System.DateTimeOffset.UtcNow))
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
                    CreateDate = t.CreateDate
                }).ToList();

            return Result<List<TestListDto>>.Success(dtos);
        }
    }
}
