using System;
using System.Collections.Generic;
using Arsis.Domain.Enums;

namespace Arsis.Application.DTOs.Tests;

public sealed record UserTestAttemptDto
{
    public Guid Id { get; init; }
    public Guid TestId { get; init; }
    public string TestTitle { get; init; } = null!;
    public TestType TestType { get; init; }
    public string UserId { get; init; } = null!;
    public string? UserFullName { get; init; }
    public string? UserEmail { get; init; }
    public string? UserPhoneNumber { get; init; }
    public int AttemptNumber { get; init; }
    public TestAttemptStatus Status { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public decimal? TotalScore { get; init; }
    public decimal? MaxScore { get; init; }
    public decimal? ScorePercentage { get; init; }
    public bool? IsPassed { get; init; }
    public int TimeElapsedMinutes { get; init; }
    public int? TimeRemainingMinutes { get; init; }
    public Guid? InvoiceId { get; init; }
}

public sealed record UserTestAttemptDetailDto
{
    public Guid Id { get; init; }
    public Guid TestId { get; init; }
    public string TestTitle { get; init; } = null!;
    public TestType TestType { get; init; }
    public string UserId { get; init; } = null!;
    public string? UserFullName { get; init; }
    public string? UserEmail { get; init; }
    public string? UserPhoneNumber { get; init; }
    public int AttemptNumber { get; init; }
    public TestAttemptStatus Status { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public decimal? TotalScore { get; init; }
    public decimal? MaxScore { get; init; }
    public decimal? ScorePercentage { get; init; }
    public bool? IsPassed { get; init; }
    public List<UserTestAnswerDto> Answers { get; init; } = new();
    public List<TestResultDto> Results { get; init; } = new();
}

public sealed record UserTestAnswerDto
{
    public Guid Id { get; init; }
    public Guid QuestionId { get; init; }
    public string QuestionText { get; init; } = null!;
    public Guid? SelectedOptionId { get; init; }
    public string? SelectedOptionText { get; init; }
    public string? TextAnswer { get; init; }
    public int? LikertValue { get; init; }
    public bool? IsCorrect { get; init; }
    public decimal? Score { get; init; }
    public DateTimeOffset AnsweredAt { get; init; }
}

public sealed record TestResultDto
{
    public Guid Id { get; init; }
    public string ResultType { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Description { get; init; } = null!;
    public decimal Score { get; init; }
    public int? Rank { get; init; }
    public string? AdditionalData { get; init; }
}
