using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Arsis.Domain.Base;
using Arsis.Domain.Enums;
using Arsis.Domain.Exceptions;

namespace Arsis.Domain.Entities.Tests;

/// <summary>
/// سوال تست
/// </summary>
public sealed class TestQuestion : Entity
{
    private readonly List<TestQuestionOption> _options = new();

    public Guid TestId { get; private set; }

    public string Text { get; private set; } = null!;

    public TestQuestionType QuestionType { get; private set; }

    /// <summary>
    /// ترتیب نمایش
    /// </summary>
    public int Order { get; private set; }

    /// <summary>
    /// امتیاز سوال (برای محاسبه نمره کل)
    /// </summary>
    public int? Score { get; private set; }

    /// <summary>
    /// آیا پاسخ به این سوال اجباری است
    /// </summary>
    public bool IsRequired { get; private set; }

    /// <summary>
    /// آدرس تصویر سوال (اختیاری)
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// توضیحات یا راهنما برای سوال
    /// </summary>
    public string? Explanation { get; private set; }

    public IReadOnlyCollection<TestQuestionOption> Options => _options.AsReadOnly();

    [ForeignKey(nameof(TestId))]
    public Test Test { get; private set; } = null!;

    [SetsRequiredMembers]
    private TestQuestion()
    {
        Text = string.Empty;
    }

    [SetsRequiredMembers]
    internal TestQuestion(
        Test test,
        string text,
        TestQuestionType questionType,
        int order,
        int? score = null,
        bool isRequired = true,
        string? imageUrl = null,
        string? explanation = null)
    {
        Test = test ?? throw new ArgumentNullException(nameof(test));
        TestId = test.Id;
        SetText(text);
        QuestionType = questionType;
        Order = order;
        Score = score;
        IsRequired = isRequired;
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new DomainException("متن سوال نمی‌تواند خالی باشد.");
        }

        Text = text.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetOrder(int order)
    {
        if (order < 0)
        {
            throw new DomainException("ترتیب سوال نمی‌تواند منفی باشد.");
        }

        Order = order;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetScore(int? score)
    {
        if (score.HasValue && score.Value < 0)
        {
            throw new DomainException("امتیاز سوال نمی‌تواند منفی باشد.");
        }

        Score = score;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetIsRequired(bool isRequired)
    {
        IsRequired = isRequired;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetQuestionType(TestQuestionType questionType)
    {
        if (QuestionType == questionType)
        {
            return;
        }

        QuestionType = questionType;
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

    public TestQuestionOption AddOption(
        string text,
        bool isCorrect = false,
        int? score = null,
        string? imageUrl = null,
        string? explanation = null)
    {
        var option = new TestQuestionOption(this, text, isCorrect, score, imageUrl, explanation);
        _options.Add(option);
        UpdateDate = DateTimeOffset.UtcNow;
        return option;
    }

    public void RemoveOption(Guid optionId)
    {
        var option = _options.FirstOrDefault(o => o.Id == optionId);
        if (option != null)
        {
            _options.Remove(option);
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }

    public void ClearOptions()
    {
        _options.Clear();
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
