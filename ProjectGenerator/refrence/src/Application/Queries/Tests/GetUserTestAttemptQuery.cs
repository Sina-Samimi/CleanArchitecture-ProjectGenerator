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

public sealed record GetUserTestAttemptQuery(Guid AttemptId) : IQuery<UserTestAttemptDetailDto>
{
    public sealed class Handler : IQueryHandler<GetUserTestAttemptQuery, UserTestAttemptDetailDto>
    {
        private readonly IUserTestAttemptRepository _attemptRepository;
        private readonly ITestResultRepository _resultRepository;

        public Handler(
            IUserTestAttemptRepository attemptRepository,
            ITestResultRepository resultRepository)
        {
            _attemptRepository = attemptRepository;
            _resultRepository = resultRepository;
        }

        public async Task<Result<UserTestAttemptDetailDto>> Handle(GetUserTestAttemptQuery request, CancellationToken cancellationToken)
        {
            var attempt = await _attemptRepository.GetByIdWithAnswersAsync(
                request.AttemptId,
                cancellationToken,
                includeDetails: true);
            if (attempt is null)
            {
                return Result<UserTestAttemptDetailDto>.Failure("آزمون مورد نظر یافت نشد.");
            }

            var results = await _resultRepository.GetByAttemptIdAsync(attempt.Id, cancellationToken);

            var dto = new UserTestAttemptDetailDto
            {
                Id = attempt.Id,
                TestId = attempt.TestId,
                TestTitle = attempt.Test?.Title ?? string.Empty,
                TestType = attempt.Test?.Type ?? TestType.General,
                UserId = attempt.UserId,
                UserFullName = attempt.User?.FullName,
                UserEmail = attempt.User?.Email,
                UserPhoneNumber = attempt.User?.PhoneNumber,
                AttemptNumber = attempt.AttemptNumber,
                Status = attempt.Status,
                StartedAt = attempt.StartedAt,
                CompletedAt = attempt.CompletedAt,
                ExpiresAt = attempt.ExpiresAt,
                TotalScore = attempt.TotalScore,
                MaxScore = attempt.MaxScore,
                ScorePercentage = attempt.ScorePercentage,
                IsPassed = attempt.IsPassed,
                Answers = attempt.Answers.Select(a => new UserTestAnswerDto
                {
                    Id = a.Id,
                    QuestionId = a.QuestionId,
                    QuestionText = a.Question?.Text ?? string.Empty,
                    SelectedOptionId = a.SelectedOptionId,
                    SelectedOptionText = a.SelectedOption?.Text,
                    TextAnswer = a.TextAnswer,
                    LikertValue = a.LikertValue,
                    IsCorrect = a.IsCorrect,
                    Score = a.Score,
                    AnsweredAt = a.AnsweredAt
                }).ToList(),
                Results = results.Select(r => new TestResultDto
                {
                    Id = r.Id,
                    ResultType = r.ResultType,
                    Title = r.Title,
                    Description = r.Description,
                    Score = r.Score,
                    Rank = r.Rank,
                    AdditionalData = r.AdditionalData
                }).ToList()
            };

            return Result<UserTestAttemptDetailDto>.Success(dto);
        }
    }
}
