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

public sealed record GetTestAttemptListQuery(
    Guid? TestId,
    string? UserId,
    TestAttemptStatus? Status,
    string? SearchTerm,
    DateTimeOffset? StartedFrom,
    DateTimeOffset? StartedTo,
    int Page,
    int PageSize) : IQuery<PagedUserTestAttemptsDto>
{
    public sealed class Handler : IQueryHandler<GetTestAttemptListQuery, PagedUserTestAttemptsDto>
    {
        private readonly IUserTestAttemptRepository _attemptRepository;

        public Handler(IUserTestAttemptRepository attemptRepository)
        {
            _attemptRepository = attemptRepository;
        }

        public async Task<Result<PagedUserTestAttemptsDto>> Handle(
            GetTestAttemptListQuery request,
            CancellationToken cancellationToken)
        {
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 20 : Math.Clamp(request.PageSize, 5, 100);

            var (attempts, totalCount, statistics) = await _attemptRepository.GetPagedAsync(
                page,
                pageSize,
                request.TestId,
                request.UserId,
                request.Status,
                request.StartedFrom,
                request.StartedTo,
                request.SearchTerm,
                cancellationToken);

            var items = attempts.Select(attempt =>
            {
                var completedDuration = attempt.CompletedAt.HasValue
                    ? (int)Math.Round((attempt.CompletedAt.Value - attempt.StartedAt).TotalMinutes)
                    : (int)Math.Round((DateTimeOffset.UtcNow - attempt.StartedAt).TotalMinutes);

                var timeElapsed = Math.Max(0, completedDuration);
                int? timeRemaining = null;

                if (attempt.ExpiresAt.HasValue && attempt.Status == TestAttemptStatus.InProgress)
                {
                    var remaining = (attempt.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalMinutes;
                    timeRemaining = remaining > 0 ? (int)Math.Ceiling(remaining) : 0;
                }

                return new UserTestAttemptDto
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
                    TimeElapsedMinutes = timeElapsed,
                    TimeRemainingMinutes = timeRemaining,
                    InvoiceId = attempt.InvoiceId
                };
            }).ToList();

            var result = new PagedUserTestAttemptsDto
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Statistics = statistics ?? TestAttemptStatisticsDto.Empty
            };

            return Result<PagedUserTestAttemptsDto>.Success(result);
        }
    }
}
