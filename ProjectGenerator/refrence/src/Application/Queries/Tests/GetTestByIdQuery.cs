using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Tests;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;
using Arsis.Domain.Enums;

namespace Arsis.Application.Queries.Tests;

public sealed record GetTestByIdQuery(Guid Id, string? UserId = null) : IQuery<TestDetailDto>
{
    public sealed class Handler : IQueryHandler<GetTestByIdQuery, TestDetailDto>
    {
        private readonly ITestRepository _testRepository;
        private readonly IUserTestAttemptRepository _attemptRepository;

        public Handler(ITestRepository testRepository, IUserTestAttemptRepository attemptRepository)
        {
            _testRepository = testRepository;
            _attemptRepository = attemptRepository;
        }

        public async Task<Result<TestDetailDto>> Handle(GetTestByIdQuery request, CancellationToken cancellationToken)
        {
            var test = await _testRepository.GetByIdWithQuestionsAsync(request.Id, cancellationToken);
            if (test is null)
            {
                return Result<TestDetailDto>.Failure("تست مورد نظر یافت نشد.");
            }

            int userAttemptsCount = 0;
            UserTestAttemptSummaryDto? latestAttemptDto = null;
            if (!string.IsNullOrEmpty(request.UserId))
            {
                userAttemptsCount = await _attemptRepository.GetUserAttemptCountAsync(
                    request.UserId,
                    test.Id,
                    cancellationToken: cancellationToken);

                var latestAttempt = await _attemptRepository.GetLatestAttemptAsync(
                    request.UserId,
                    test.Id,
                    cancellationToken);

                if (latestAttempt is not null)
                {
                    var effectiveStatus = latestAttempt.Status;
                    if (effectiveStatus == TestAttemptStatus.InProgress && latestAttempt.CompletedAt.HasValue)
                    {
                        effectiveStatus = TestAttemptStatus.Completed;
                    }

                    latestAttemptDto = new UserTestAttemptSummaryDto
                    {
                        Id = latestAttempt.Id,
                        Status = effectiveStatus,
                        StartedAt = latestAttempt.StartedAt,
                        CompletedAt = latestAttempt.CompletedAt
                    };
                }
            }

            var dto = new TestDetailDto
            {
                Id = test.Id,
                Title = test.Title,
                Description = test.Description,
                Type = test.Type,
                Status = test.Status,
                CategoryId = test.CategoryId,
                CategoryName = test.Category?.Name,
                Price = test.Price,
                Currency = test.Currency ?? "IRT",
                DurationMinutes = test.DurationMinutes,
                MaxAttempts = test.MaxAttempts,
                ShowResultsImmediately = test.ShowResultsImmediately,
                ShowCorrectAnswers = test.ShowCorrectAnswers,
                RandomizeQuestions = test.RandomizeQuestions,
                RandomizeOptions = test.RandomizeOptions,
                AvailableFrom = test.AvailableFrom,
                AvailableUntil = test.AvailableUntil,
                NumberOfQuestionsToShow = test.NumberOfQuestionsToShow,
                PassingScore = test.PassingScore,
                Questions = test.Questions.Select(q => new TestQuestionDto
                {
                    Id = q.Id,
                    TestId = q.TestId,
                    Text = q.Text,
                    QuestionType = q.QuestionType,
                    Order = q.Order,
                    Score = q.Score,
                    IsRequired = q.IsRequired,
                    ImageUrl = q.ImageUrl,
                    Explanation = q.Explanation,
                    Options = q.Options.Select(o => new TestQuestionOptionDto
                    {
                        Id = o.Id,
                        QuestionId = o.QuestionId,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect,
                        Score = o.Score,
                        ImageUrl = o.ImageUrl,
                        Explanation = o.Explanation,
                        Order = o.Order
                    }).ToList()
                }).ToList(),
                IsAvailable = test.IsAvailable(DateTimeOffset.UtcNow),
                CanUserAttempt = string.IsNullOrEmpty(request.UserId) || test.CanUserAttempt(request.UserId),
                UserAttemptsCount = userAttemptsCount,
                LatestUserAttempt = latestAttemptDto
            };

            return Result<TestDetailDto>.Success(dto);
        }
    }
}
