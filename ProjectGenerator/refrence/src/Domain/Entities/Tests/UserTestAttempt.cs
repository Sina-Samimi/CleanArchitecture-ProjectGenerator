using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Arsis.Domain.Base;
using Arsis.Domain.Enums;
using Arsis.Domain.Exceptions;
using Arsis.Domain.Interfaces;

namespace Arsis.Domain.Entities.Tests;

/// <summary>
/// شرکت کاربر در تست
/// </summary>
public sealed class UserTestAttempt : Entity, IAggregateRoot
{
    private readonly List<UserTestAnswer> _answers = new();

    public Guid TestId { get; private set; }

    public string UserId { get; private set; } = null!;

    /// <summary>
    /// شماره دفعه شرکت در تست
    /// </summary>
    public int AttemptNumber { get; private set; }

    public TestAttemptStatus Status { get; private set; }

    /// <summary>
    /// زمان شروع تست
    /// </summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>
    /// زمان اتمام تست
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// زمان انقضا (اگر تست محدودیت زمانی داشته باشد)
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// نمره کل (برای تست‌های عمومی)
    /// </summary>
    public decimal? TotalScore { get; private set; }

    /// <summary>
    /// حداکثر نمره ممکن
    /// </summary>
    public decimal? MaxScore { get; private set; }

    /// <summary>
    /// درصد نمره
    /// </summary>
    public decimal? ScorePercentage { get; private set; }

    /// <summary>
    /// آیا قبول شده است (بر اساس حداقل نمره قبولی)
    /// </summary>
    public bool? IsPassed { get; private set; }

    /// <summary>
    /// شناسه فاکتور پرداخت
    /// </summary>
    public Guid? InvoiceId { get; private set; }

    public IReadOnlyCollection<UserTestAnswer> Answers => _answers.AsReadOnly();

    [ForeignKey(nameof(TestId))]
    public Test Test { get; private set; } = null!;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; private set; } = null!;

    [SetsRequiredMembers]
    private UserTestAttempt()
    {
        UserId = string.Empty;
        Status = TestAttemptStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
    }

    [SetsRequiredMembers]
    public UserTestAttempt(
        Test test,
        string userId,
        int attemptNumber,
        Guid? invoiceId = null,
        int? durationMinutes = null)
    {
        Test = test ?? throw new ArgumentNullException(nameof(test));
        TestId = test.Id;
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("شناسه کاربر نمی‌تواند خالی باشد.");
        }
        
        UserId = userId;
        AttemptNumber = attemptNumber;
        InvoiceId = invoiceId;
        Status = TestAttemptStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
        
        if (durationMinutes.HasValue)
        {
            ExpiresAt = StartedAt.AddMinutes(durationMinutes.Value);
        }
        
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public UserTestAnswer AddAnswer(
        Guid questionId,
        Guid? selectedOptionId = null,
        string? textAnswer = null,
        int? likertValue = null)
    {
        if (Status != TestAttemptStatus.InProgress)
        {
            throw new DomainException("فقط می‌توان به تست‌های در حال انجام پاسخ داد.");
        }

        if (ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value)
        {
            Status = TestAttemptStatus.Expired;
            UpdateDate = DateTimeOffset.UtcNow;
            throw new DomainException("زمان تست به پایان رسیده است.");
        }

        var existingAnswer = _answers.FirstOrDefault(a => a.QuestionId == questionId);
        if (existingAnswer != null)
        {
            existingAnswer.UpdateAnswer(selectedOptionId, textAnswer, likertValue);
        }
        else
        {
            var answer = new UserTestAnswer(this, questionId, selectedOptionId, textAnswer, likertValue);
            _answers.Add(answer);
        }

        UpdateDate = DateTimeOffset.UtcNow;
        return existingAnswer ?? _answers.Last();
    }

    public void Complete()
    {
        if (Status != TestAttemptStatus.InProgress)
        {
            throw new DomainException("فقط می‌توان تست‌های در حال انجام را تکمیل کرد.");
        }

        Status = TestAttemptStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == TestAttemptStatus.Completed)
        {
            throw new DomainException("نمی‌توان تست تکمیل شده را لغو کرد.");
        }

        Status = TestAttemptStatus.Cancelled;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void CalculateScore(decimal totalScore, decimal maxScore, decimal? passingScore = null)
    {
        if (Status != TestAttemptStatus.Completed)
        {
            throw new DomainException("فقط می‌توان نمره تست‌های تکمیل شده را محاسبه کرد.");
        }

        TotalScore = totalScore;
        MaxScore = maxScore;
        
        if (maxScore > 0)
        {
            ScorePercentage = Math.Round((totalScore / maxScore) * 100, 2);
        }

        if (passingScore.HasValue && maxScore > 0)
        {
            // If PassingScore is greater than maxScore, treat it as a percentage
            // For example: if maxScore=2 but passingScore=50, treat 50 as 50% not absolute score
            if (passingScore.Value > maxScore && passingScore.Value <= 100)
            {
                // PassingScore is likely a percentage (e.g., 50 means 50%)
                var requiredPercentage = passingScore.Value;
                IsPassed = ScorePercentage >= requiredPercentage;
            }
            else
            {
                // PassingScore is an absolute score
                IsPassed = totalScore >= passingScore.Value;
            }
        }

        UpdateDate = DateTimeOffset.UtcNow;
    }

    public void CheckExpiration()
    {
        if (Status == TestAttemptStatus.InProgress && 
            ExpiresAt.HasValue && 
            DateTimeOffset.UtcNow > ExpiresAt.Value)
        {
            Status = TestAttemptStatus.Expired;
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }
}
