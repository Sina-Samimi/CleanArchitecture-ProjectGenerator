using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;

namespace Arsis.Domain.Entities.Tests;

/// <summary>
/// پاسخ کاربر به سوال تست
/// </summary>
public sealed class UserTestAnswer : Entity
{
    public Guid AttemptId { get; private set; }

    public Guid QuestionId { get; private set; }

    /// <summary>
    /// گزینه انتخاب شده (برای سوالات چند گزینه‌ای)
    /// </summary>
    public Guid? SelectedOptionId { get; private set; }

    /// <summary>
    /// پاسخ متنی (برای سوالات متنی)
    /// </summary>
    public string? TextAnswer { get; private set; }

    /// <summary>
    /// مقدار لیکرت (برای سوالات مقیاس لیکرت)
    /// </summary>
    public int? LikertValue { get; private set; }

    /// <summary>
    /// آیا پاسخ صحیح است
    /// </summary>
    public bool? IsCorrect { get; private set; }

    /// <summary>
    /// امتیاز کسب شده
    /// </summary>
    public decimal? Score { get; private set; }

    /// <summary>
    /// زمان پاسخ
    /// </summary>
    public DateTimeOffset AnsweredAt { get; private set; }

    [ForeignKey(nameof(AttemptId))]
    public UserTestAttempt Attempt { get; private set; } = null!;

    [ForeignKey(nameof(QuestionId))]
    public TestQuestion Question { get; private set; } = null!;

    [ForeignKey(nameof(SelectedOptionId))]
    public TestQuestionOption? SelectedOption { get; private set; }

    [SetsRequiredMembers]
    private UserTestAnswer()
    {
        AnsweredAt = DateTimeOffset.UtcNow;
    }

    [SetsRequiredMembers]
    internal UserTestAnswer(
        UserTestAttempt attempt,
        Guid questionId,
        Guid? selectedOptionId = null,
        string? textAnswer = null,
        int? likertValue = null)
    {
        Attempt = attempt ?? throw new ArgumentNullException(nameof(attempt));
        AttemptId = attempt.Id;
        QuestionId = questionId;
        SelectedOptionId = selectedOptionId;
        TextAnswer = string.IsNullOrWhiteSpace(textAnswer) ? null : textAnswer.Trim();
        LikertValue = likertValue;
        AnsweredAt = DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateAnswer(
        Guid? selectedOptionId = null,
        string? textAnswer = null,
        int? likertValue = null)
    {
        SelectedOptionId = selectedOptionId;
        TextAnswer = string.IsNullOrWhiteSpace(textAnswer) ? null : textAnswer.Trim();
        LikertValue = likertValue;
        AnsweredAt = DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetCorrectness(bool isCorrect)
    {
        IsCorrect = isCorrect;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetScore(decimal score)
    {
        Score = score;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
