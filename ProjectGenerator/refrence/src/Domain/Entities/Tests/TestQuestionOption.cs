using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;
using Arsis.Domain.Exceptions;

namespace Arsis.Domain.Entities.Tests;

/// <summary>
/// گزینه سوال تست
/// </summary>
public sealed class TestQuestionOption : Entity
{
    public Guid QuestionId { get; private set; }

    public string Text { get; private set; } = null!;

    /// <summary>
    /// آیا این گزینه پاسخ صحیح است
    /// </summary>
    public bool IsCorrect { get; private set; }

    /// <summary>
    /// امتیاز این گزینه (برای تست‌های روانشناسی مانند DISC)
    /// </summary>
    public int? Score { get; private set; }

    /// <summary>
    /// آدرس تصویر گزینه (اختیاری)
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// توضیح یا توجیه گزینه (اختیاری)
    /// </summary>
    public string? Explanation { get; private set; }

    /// <summary>
    /// ترتیب نمایش
    /// </summary>
    public int Order { get; private set; }

    [ForeignKey(nameof(QuestionId))]
    public TestQuestion Question { get; private set; } = null!;

    [SetsRequiredMembers]
    private TestQuestionOption()
    {
        Text = string.Empty;
    }

    [SetsRequiredMembers]
    internal TestQuestionOption(
        TestQuestion question,
        string text,
        bool isCorrect = false,
        int? score = null,
        string? imageUrl = null,
        string? explanation = null)
    {
        Question = question ?? throw new ArgumentNullException(nameof(question));
        QuestionId = question.Id;
        SetText(text);
        IsCorrect = isCorrect;
        Score = score;
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainException("متن گزینه نمی‌تواند خالی باشد.");
        }

        Text = text.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetIsCorrect(bool isCorrect)
    {
        IsCorrect = isCorrect;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetScore(int? score)
    {
        Score = score;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetImageUrl(string? imageUrl)
    {
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetExplanation(string? explanation)
    {
        Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetOrder(int order)
    {
        Order = order;
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
