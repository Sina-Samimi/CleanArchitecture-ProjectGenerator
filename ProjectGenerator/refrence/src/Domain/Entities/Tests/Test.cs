using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Arsis.Domain.Base;
using Arsis.Domain.Enums;
using Arsis.Domain.Exceptions;
using Arsis.Domain.Interfaces;

namespace Arsis.Domain.Entities.Tests;

/// <summary>
/// تست / آزمون
/// </summary>
public sealed class Test : Entity, IAggregateRoot
{
    private readonly List<TestQuestion> _questions = new();
    private readonly List<UserTestAttempt> _attempts = new();

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public TestType Type { get; private set; }

    public TestStatus Status { get; private set; }

    /// <summary>
    /// دسته‌بندی تست
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// قیمت تست (برای پرداخت)
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// واحد پول تست
    /// </summary>
    public string Currency { get; private set; } = "IRT";

    /// <summary>
    /// مدت زمان تست به دقیقه (null = بدون محدودیت)
    /// </summary>
    public int? DurationMinutes { get; private set; }

    /// <summary>
    /// تعداد دفعات مجاز برای شرکت در تست (null = نامحدود)
    /// </summary>
    public int? MaxAttempts { get; private set; }

    /// <summary>
    /// نمایش نتایج بلافاصله بعد از تکمیل
    /// </summary>
    public bool ShowResultsImmediately { get; private set; }

    /// <summary>
    /// نمایش پاسخ‌های صحیح
    /// </summary>
    public bool ShowCorrectAnswers { get; private set; }

    /// <summary>
    /// آیا سوالات به صورت تصادفی نمایش داده شوند
    /// </summary>
    public bool RandomizeQuestions { get; private set; }

    /// <summary>
    /// آیا گزینه‌ها به صورت تصادفی نمایش داده شوند
    /// </summary>
    public bool RandomizeOptions { get; private set; }

    /// <summary>
    /// تاریخ شروع دسترسی
    /// </summary>
    public DateTimeOffset? AvailableFrom { get; private set; }

    /// <summary>
    /// تاریخ پایان دسترسی
    /// </summary>
    public DateTimeOffset? AvailableUntil { get; private set; }

    /// <summary>
    /// تعداد سوالات قابل نمایش (برای تست‌های بانک سوالی، null = همه سوالات)
    /// </summary>
    public int? NumberOfQuestionsToShow { get; private set; }

    /// <summary>
    /// حداقل نمره قبولی (برای تست‌های عمومی)
    /// </summary>
    public decimal? PassingScore { get; private set; }

    public IReadOnlyCollection<TestQuestion> Questions => _questions.AsReadOnly();
    
    public IReadOnlyCollection<UserTestAttempt> Attempts => _attempts.AsReadOnly();

    public Catalog.SiteCategory? Category { get; private set; }

    [SetsRequiredMembers]
    private Test()
    {
        Title = string.Empty;
        Description = string.Empty;
        Status = TestStatus.Draft;
    }

    [SetsRequiredMembers]
    public Test(
        string title,
        string description,
        TestType type,
        decimal price,
        string currency = "IRT",
        Guid? categoryId = null,
        int? durationMinutes = null,
        int? maxAttempts = null,
        bool showResultsImmediately = true,
        bool showCorrectAnswers = false,
        bool randomizeQuestions = false,
        bool randomizeOptions = false,
        int? numberOfQuestionsToShow = null,
        decimal? passingScore = null)
    {
        SetTitle(title);
        SetDescription(description);
        Type = type;
        CategoryId = categoryId;
        SetPrice(price);
        SetCurrency(currency);
        SetDuration(durationMinutes);
        MaxAttempts = maxAttempts;
        ShowResultsImmediately = showResultsImmediately;
        ShowCorrectAnswers = showCorrectAnswers;
        RandomizeQuestions = randomizeQuestions;
        RandomizeOptions = randomizeOptions;
        NumberOfQuestionsToShow = numberOfQuestionsToShow;
        PassingScore = passingScore;
        Status = TestStatus.Draft;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("عنوان تست نمی‌تواند خالی باشد.");
        }

        Title = title.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetDescription(string description)
    {
        Description = description?.Trim() ?? string.Empty;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetType(TestType type)
    {
        if (Type == type)
        {
            return;
        }

        Type = type;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetPrice(decimal price)
    {
        if (price < 0)
        {
            throw new DomainException("قیمت نمی‌تواند منفی باشد.");
        }

        Price = price;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            currency = "IRT";
        }

        Currency = currency.Trim().ToUpperInvariant();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetDuration(int? durationMinutes)
    {
        if (durationMinutes.HasValue && durationMinutes.Value <= 0)
        {
            throw new DomainException("مدت زمان تست باید بزرگتر از صفر باشد.");
        }

        DurationMinutes = durationMinutes;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetCategory(Guid? categoryId)
    {
        CategoryId = categoryId;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void SetAvailability(DateTimeOffset? availableFrom, DateTimeOffset? availableUntil)
    {
        if (availableFrom.HasValue && availableUntil.HasValue && availableFrom.Value >= availableUntil.Value)
        {
            throw new DomainException("تاریخ شروع باید قبل از تاریخ پایان باشد.");
        }

        AvailableFrom = availableFrom;
        AvailableUntil = availableUntil;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Publish()
    {
        if (!_questions.Any())
        {
            throw new DomainException("نمی‌توان تستی را بدون سوال منتشر کرد.");
        }

        Status = TestStatus.Published;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        Status = TestStatus.Archived;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UnPublish()
    {
        Status = TestStatus.Draft;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public TestQuestion AddQuestion(
        string text,
        TestQuestionType questionType,
        int order,
        int? score = null,
        bool isRequired = true,
        string? imageUrl = null,
        string? explanation = null)
    {
        var question = new TestQuestion(this, text, questionType, order, score, isRequired, imageUrl, explanation);
        _questions.Add(question);
        UpdateDate = DateTimeOffset.UtcNow;
        return question;
    }

    public void RemoveQuestion(Guid questionId)
    {
        var question = _questions.FirstOrDefault(q => q.Id == questionId);
        if (question != null)
        {
            _questions.Remove(question);
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }

    public bool IsAvailable(DateTimeOffset now)
    {
        if (Status != TestStatus.Published)
        {
            return false;
        }

        if (AvailableFrom.HasValue && now < AvailableFrom.Value)
        {
            return false;
        }

        if (AvailableUntil.HasValue && now > AvailableUntil.Value)
        {
            return false;
        }

        return true;
    }

    public bool CanUserAttempt(string userId)
    {
        if (!MaxAttempts.HasValue)
        {
            return true;
        }

        var userAttempts = _attempts.Count(a => a.UserId == userId && a.Status == TestAttemptStatus.Completed);
        return userAttempts < MaxAttempts.Value;
    }
}
