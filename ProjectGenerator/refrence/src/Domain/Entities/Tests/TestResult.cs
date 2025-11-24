using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;
using Arsis.Domain.Interfaces;

namespace Arsis.Domain.Entities.Tests;

/// <summary>
/// نتیجه تست (برای تست‌های روانشناسی مانند DISC، کلیفتون و ...)
/// </summary>
public sealed class TestResult : Entity, IAggregateRoot
{
    public Guid AttemptId { get; private set; }

    /// <summary>
    /// نوع نتیجه (مثلاً Dominance, Influence, Steadiness, Conscientiousness برای DISC)
    /// </summary>
    public string ResultType { get; private set; } = null!;

    /// <summary>
    /// عنوان نتیجه
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// توضیحات نتیجه
    /// </summary>
    public string Description { get; private set; } = null!;

    /// <summary>
    /// امتیاز یا درصد
    /// </summary>
    public decimal Score { get; private set; }

    /// <summary>
    /// رتبه (اگر قابل رتبه‌بندی باشد)
    /// </summary>
    public int? Rank { get; private set; }

    /// <summary>
    /// داده‌های اضافی به صورت JSON (برای نمودارها و اطلاعات تکمیلی)
    /// </summary>
    public string? AdditionalData { get; private set; }

    [ForeignKey(nameof(AttemptId))]
    public UserTestAttempt Attempt { get; private set; } = null!;

    [SetsRequiredMembers]
    private TestResult()
    {
        ResultType = string.Empty;
        Title = string.Empty;
        Description = string.Empty;
    }

    [SetsRequiredMembers]
    public TestResult(
        UserTestAttempt attempt,
        string resultType,
        string title,
        string description,
        decimal score,
        int? rank = null,
        string? additionalData = null)
    {
        Attempt = attempt ?? throw new ArgumentNullException(nameof(attempt));
        AttemptId = attempt.Id;
        ResultType = resultType?.Trim() ?? throw new ArgumentNullException(nameof(resultType));
        Title = title?.Trim() ?? throw new ArgumentNullException(nameof(title));
        Description = description?.Trim() ?? string.Empty;
        Score = score;
        Rank = rank;
        AdditionalData = string.IsNullOrWhiteSpace(additionalData) ? null : additionalData.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateScore(decimal score)
    {
        Score = score;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateRank(int? rank)
    {
        Rank = rank;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void UpdateAdditionalData(string? additionalData)
    {
        AdditionalData = string.IsNullOrWhiteSpace(additionalData) ? null : additionalData.Trim();
        UpdateDate = DateTimeOffset.UtcNow;
    }
}
