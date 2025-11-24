using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Abstractions.Messaging;
using Arsis.Application.DTOs.Tests;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.BaseTypes;

namespace Arsis.Application.Queries.Tests;

public sealed record GetUserTestAttemptsQuery(string UserId) : IQuery<List<UserTestAttemptDto>>
{
    public sealed class Handler : IQueryHandler<GetUserTestAttemptsQuery, List<UserTestAttemptDto>>
    {
        private readonly IUserTestAttemptRepository _attemptRepository;

        public Handler(IUserTestAttemptRepository attemptRepository)
        {
            _attemptRepository = attemptRepository;
        }

        public async Task<Result<List<UserTestAttemptDto>>> Handle(GetUserTestAttemptsQuery request, CancellationToken cancellationToken)
        {
            var attempts = await _attemptRepository.GetUserAttemptsAsync(request.UserId, cancellationToken);

            var dtos = attempts.Select(a =>
            {
                var timeElapsed = (int)(DateTimeOffset.UtcNow - a.StartedAt).TotalMinutes;
                int? timeRemaining = null;
                
                if (a.ExpiresAt.HasValue && a.Status == Domain.Enums.TestAttemptStatus.InProgress)
                {
                    var remaining = (a.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalMinutes;
                    timeRemaining = remaining > 0 ? (int)Math.Ceiling(remaining) : 0;
                }

                return new UserTestAttemptDto
                {
                    Id = a.Id,
                    TestId = a.TestId,
                    TestTitle = a.Test?.Title ?? string.Empty,
                    TestType = a.Test?.Type ?? Domain.Enums.TestType.General,
                    UserId = a.UserId,
                    AttemptNumber = a.AttemptNumber,
                    Status = a.Status,
                    StartedAt = a.StartedAt,
                    CompletedAt = a.CompletedAt,
                    ExpiresAt = a.ExpiresAt,
                    TotalScore = a.TotalScore,
                    MaxScore = a.MaxScore,
                    ScorePercentage = a.ScorePercentage,
                    IsPassed = a.IsPassed,
                    TimeElapsedMinutes = timeElapsed,
                    TimeRemainingMinutes = timeRemaining,
                    InvoiceId = a.InvoiceId
                };
            }).ToList();

            return Result<List<UserTestAttemptDto>>.Success(dtos);
        }
    }
}
