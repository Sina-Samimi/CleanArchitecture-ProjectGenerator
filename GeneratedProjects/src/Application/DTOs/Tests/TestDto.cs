using TestAttarClone.Domain.Enums;

namespace TestAttarClone.Application.DTOs.Tests;

public sealed record TestDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Description { get; init; } = null!;
    public TestType Type { get; init; }
    public TestStatus Status { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "IRT";
    public int? DurationMinutes { get; init; }
    public int? MaxAttempts { get; init; }
    public bool ShowResultsImmediately { get; init; }
    public bool ShowCorrectAnswers { get; init; }
    public bool RandomizeQuestions { get; init; }
    public bool RandomizeOptions { get; init; }
    public DateTimeOffset? AvailableFrom { get; init; }
    public DateTimeOffset? AvailableUntil { get; init; }
    public int? NumberOfQuestionsToShow { get; init; }
    public decimal? PassingScore { get; init; }
    public int QuestionsCount { get; init; }
    public int AttemptsCount { get; init; }
    public DateTimeOffset CreateDate { get; init; }
    public DateTimeOffset UpdateDate { get; init; }
}

public sealed record TestListDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public TestType Type { get; init; }
    public TestStatus Status { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "IRT";
    public int QuestionsCount { get; init; }
    public int AttemptsCount { get; init; }
    public DateTimeOffset CreateDate { get; init; }
}

public sealed record TestDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = null!;
    public string Description { get; init; } = null!;
    public TestType Type { get; init; }
    public TestStatus Status { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "IRT";
    public int? DurationMinutes { get; init; }
    public int? MaxAttempts { get; init; }
    public bool ShowResultsImmediately { get; init; }
    public bool ShowCorrectAnswers { get; init; }
    public bool RandomizeQuestions { get; init; }
    public bool RandomizeOptions { get; init; }
    public DateTimeOffset? AvailableFrom { get; init; }
    public DateTimeOffset? AvailableUntil { get; init; }
    public int? NumberOfQuestionsToShow { get; init; }
    public decimal? PassingScore { get; init; }
    public List<TestQuestionDto> Questions { get; init; } = new();
    public bool IsAvailable { get; init; }
    public bool CanUserAttempt { get; init; }
    public int UserAttemptsCount { get; init; }
    public UserTestAttemptSummaryDto? LatestUserAttempt { get; init; }
}

public sealed record UserTestAttemptSummaryDto
{
    public Guid Id { get; init; }
    public TestAttemptStatus Status { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}
