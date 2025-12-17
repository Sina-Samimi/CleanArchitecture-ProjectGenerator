using TestAttarClone.Domain.Enums;

namespace TestAttarClone.Application.DTOs.Tests;

public sealed record TestQuestionDto
{
    public Guid Id { get; init; }
    public Guid TestId { get; init; }
    public string Text { get; init; } = null!;
    public TestQuestionType QuestionType { get; init; }
    public int Order { get; init; }
    public int? Score { get; init; }
    public bool IsRequired { get; init; }
    public string? ImageUrl { get; init; }
    public string? Explanation { get; init; }
    public List<TestQuestionOptionDto> Options { get; init; } = new();
}

public sealed record TestQuestionOptionDto
{
    public Guid Id { get; init; }
    public Guid QuestionId { get; init; }
    public string Text { get; init; } = null!;
    public bool IsCorrect { get; init; }
    public int? Score { get; init; }
    public string? ImageUrl { get; init; }
    public string? Explanation { get; init; }
    public int Order { get; init; }
}
