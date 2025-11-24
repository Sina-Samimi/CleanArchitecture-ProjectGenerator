using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.DTOs.Dashboard;
using Arsis.Application.Interfaces;
using Arsis.Domain.Entities;
using Arsis.Domain.Enums;
using Arsis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Arsis.Infrastructure.Services;

public sealed class AdminDashboardMetricsService : IAdminDashboardMetricsService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminDashboardMetricsService(AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<SystemPerformanceSummaryDto> GetSummaryAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken)
    {
        var now = referenceTime;
        var currentPeriodEnd = now;
        var currentPeriodStart = now.AddDays(-30);
        var previousPeriodStart = now.AddDays(-60);
        var previousPeriodEnd = currentPeriodStart;

        var currentPeriodStartUtc = currentPeriodStart.UtcDateTime;
        var currentPeriodEndUtc = currentPeriodEnd.UtcDateTime;
        var previousPeriodStartUtc = previousPeriodStart.UtcDateTime;
        var previousPeriodEndUtc = previousPeriodEnd.UtcDateTime;

        var todayDateOnly = DateOnly.FromDateTime(now.Date);
        var currentWeekStart = todayDateOnly.AddDays(-6);
        var previousWeekStart = currentWeekStart.AddDays(-7);

        var people = await BuildPeopleMetricsAsync(currentPeriodStart, currentPeriodEnd, previousPeriodStart, previousPeriodEnd, cancellationToken);
        var commerce = await BuildCommerceMetricsAsync(currentPeriodStart, currentPeriodEnd, previousPeriodStart, previousPeriodEnd, cancellationToken);
        var learning = await BuildLearningMetricsAsync(currentPeriodStart, currentPeriodEnd, previousPeriodStart, previousPeriodEnd, currentPeriodStartUtc, currentPeriodEndUtc, previousPeriodStartUtc, previousPeriodEndUtc, cancellationToken);
        var content = await BuildContentMetricsAsync(currentPeriodStart, currentPeriodEnd, previousPeriodStart, previousPeriodEnd, currentWeekStart, previousWeekStart, todayDateOnly, cancellationToken);

        var period = new PeriodWindowDto(
            currentPeriodStart,
            currentPeriodEnd,
            previousPeriodStart,
            previousPeriodEnd,
            currentWeekStart,
            previousWeekStart);

        return new SystemPerformanceSummaryDto(
            period,
            people,
            commerce,
            learning,
            content,
            now);
    }

    private async Task<PeopleMetricsDto> BuildPeopleMetricsAsync(
        DateTimeOffset currentPeriodStart,
        DateTimeOffset currentPeriodEnd,
        DateTimeOffset previousPeriodStart,
        DateTimeOffset previousPeriodEnd,
        CancellationToken cancellationToken)
    {
        var usersQuery = _userManager.Users
            .AsNoTracking()
            .Where(user => !user.IsDeleted);

        var totalUsers = await usersQuery.CountAsync(cancellationToken);
        var activeUsers = await usersQuery.Where(user => user.IsActive).CountAsync(cancellationToken);
        var newUsersCurrent = await usersQuery
            .Where(user => user.CreatedOn >= currentPeriodStart && user.CreatedOn <= currentPeriodEnd)
            .CountAsync(cancellationToken);
        var newUsersPrevious = await usersQuery
            .Where(user => user.CreatedOn >= previousPeriodStart && user.CreatedOn < previousPeriodEnd)
            .CountAsync(cancellationToken);

        var teachersQuery = _dbContext.TeacherProfiles
            .AsNoTracking()
            .Where(teacher => !teacher.IsDeleted);

        var totalTeachers = await teachersQuery.CountAsync(cancellationToken);
        var activeTeachers = await teachersQuery.Where(teacher => teacher.IsActive).CountAsync(cancellationToken);
        var newTeachersCurrent = await teachersQuery
            .Where(teacher => teacher.CreateDate >= currentPeriodStart && teacher.CreateDate <= currentPeriodEnd)
            .CountAsync(cancellationToken);
        var newTeachersPrevious = await teachersQuery
            .Where(teacher => teacher.CreateDate >= previousPeriodStart && teacher.CreateDate < previousPeriodEnd)
            .CountAsync(cancellationToken);

        return new PeopleMetricsDto(
            totalUsers,
            activeUsers,
            newUsersCurrent,
            newUsersPrevious,
            totalTeachers,
            activeTeachers,
            newTeachersCurrent,
            newTeachersPrevious);
    }

    private async Task<CommerceMetricsDto> BuildCommerceMetricsAsync(
        DateTimeOffset currentPeriodStart,
        DateTimeOffset currentPeriodEnd,
        DateTimeOffset previousPeriodStart,
        DateTimeOffset previousPeriodEnd,
        CancellationToken cancellationToken)
    {
        var paymentsQuery = _dbContext.PaymentTransactions.AsNoTracking();
        var succeededPaymentsQuery = paymentsQuery.Where(transaction => transaction.Status == TransactionStatus.Succeeded);

        var revenueAllTime = await succeededPaymentsQuery
            .Select(transaction => (decimal?)transaction.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var revenueCurrent = await succeededPaymentsQuery
            .Where(transaction => transaction.OccurredAt >= currentPeriodStart && transaction.OccurredAt <= currentPeriodEnd)
            .Select(transaction => (decimal?)transaction.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var revenuePrevious = await succeededPaymentsQuery
            .Where(transaction => transaction.OccurredAt >= previousPeriodStart && transaction.OccurredAt < previousPeriodEnd)
            .Select(transaction => (decimal?)transaction.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var successfulTransactionsCurrent = await succeededPaymentsQuery
            .Where(transaction => transaction.OccurredAt >= currentPeriodStart && transaction.OccurredAt <= currentPeriodEnd)
            .CountAsync(cancellationToken);

        var successfulTransactionsPrevious = await succeededPaymentsQuery
            .Where(transaction => transaction.OccurredAt >= previousPeriodStart && transaction.OccurredAt < previousPeriodEnd)
            .CountAsync(cancellationToken);

        var failedTransactionsCurrent = await paymentsQuery
            .Where(transaction => transaction.Status == TransactionStatus.Failed &&
                                  transaction.OccurredAt >= currentPeriodStart &&
                                  transaction.OccurredAt <= currentPeriodEnd)
            .CountAsync(cancellationToken);

        var pendingInvoices = await _dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => !invoice.IsDeleted &&
                              (invoice.Status == InvoiceStatus.Pending ||
                               invoice.Status == InvoiceStatus.PartiallyPaid))
            .CountAsync(cancellationToken);

        var overdueInvoices = await _dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => !invoice.IsDeleted && invoice.Status == InvoiceStatus.Overdue)
            .CountAsync(cancellationToken);

        var successfulCurrent = successfulTransactionsCurrent;
        var successfulPrevious = successfulTransactionsPrevious;

        var averageOrderValueCurrent = successfulCurrent == 0
            ? 0m
            : decimal.Round(revenueCurrent / successfulCurrent, 2, MidpointRounding.AwayFromZero);

        var averageOrderValuePrevious = successfulPrevious == 0
            ? 0m
            : decimal.Round(revenuePrevious / successfulPrevious, 2, MidpointRounding.AwayFromZero);

        return new CommerceMetricsDto(
            revenueAllTime,
            revenueCurrent,
            revenuePrevious,
            averageOrderValueCurrent,
            averageOrderValuePrevious,
            successfulCurrent,
            successfulPrevious,
            failedTransactionsCurrent,
            pendingInvoices,
            overdueInvoices);
    }

    private async Task<LearningMetricsDto> BuildLearningMetricsAsync(
        DateTimeOffset currentPeriodStart,
        DateTimeOffset currentPeriodEnd,
        DateTimeOffset previousPeriodStart,
        DateTimeOffset previousPeriodEnd,
        DateTime currentPeriodStartUtc,
        DateTime currentPeriodEndUtc,
        DateTime previousPeriodStartUtc,
        DateTime previousPeriodEndUtc,
        CancellationToken cancellationToken)
    {
        var testsCount = await _dbContext.Tests
            .AsNoTracking()
            .Where(test => !test.IsDeleted)
            .CountAsync(cancellationToken);

        var activeAttempts = await _dbContext.UserTestAttempts
            .AsNoTracking()
            .Where(attempt => attempt.Status == TestAttemptStatus.InProgress)
            .CountAsync(cancellationToken);

        var completedAttemptsCurrent = await _dbContext.UserTestAttempts
            .AsNoTracking()
            .Where(attempt =>
                attempt.Status == TestAttemptStatus.Completed &&
                attempt.CompletedAt.HasValue &&
                attempt.CompletedAt.Value >= currentPeriodStart &&
                attempt.CompletedAt.Value <= currentPeriodEnd)
            .Select(attempt => new AttemptScoreProjection(
                attempt.CompletedAt!.Value,
                attempt.ScorePercentage))
            .ToListAsync(cancellationToken);

        var completedAttemptsPrevious = await _dbContext.UserTestAttempts
            .AsNoTracking()
            .Where(attempt =>
                attempt.Status == TestAttemptStatus.Completed &&
                attempt.CompletedAt.HasValue &&
                attempt.CompletedAt.Value >= previousPeriodStart &&
                attempt.CompletedAt.Value < previousPeriodEnd)
            .Select(attempt => new AttemptScoreProjection(
                attempt.CompletedAt!.Value,
                attempt.ScorePercentage))
            .ToListAsync(cancellationToken);

        var assessmentsCompletedCurrent = await _dbContext.AssessmentRuns
            .AsNoTracking()
            .Where(run => run.CompletedAt.HasValue &&
                          run.CompletedAt.Value >= currentPeriodStartUtc &&
                          run.CompletedAt.Value <= currentPeriodEndUtc)
            .Select(run => new AssessmentDurationProjection(
                run.StartedAt,
                run.CompletedAt!.Value))
            .ToListAsync(cancellationToken);

        var assessmentsCompletedPrevious = await _dbContext.AssessmentRuns
            .AsNoTracking()
            .Where(run => run.CompletedAt.HasValue &&
                          run.CompletedAt.Value >= previousPeriodStartUtc &&
                          run.CompletedAt.Value < previousPeriodEndUtc)
            .Select(run => new AssessmentDurationProjection(
                run.StartedAt,
                run.CompletedAt!.Value))
            .ToListAsync(cancellationToken);

        var completedCurrent = completedAttemptsCurrent;
        var completedPrevious = completedAttemptsPrevious;

        var averageScoreCurrent = CalculateAverageScore(completedCurrent);
        var averageScorePrevious = CalculateAverageScore(completedPrevious);

        var assessmentCurrent = assessmentsCompletedCurrent;
        var assessmentPrevious = assessmentsCompletedPrevious;

        var averageAssessmentDurationCurrent = CalculateAverageAssessmentDurationMinutes(assessmentCurrent);
        var averageAssessmentDurationPrevious = CalculateAverageAssessmentDurationMinutes(assessmentPrevious);

        return new LearningMetricsDto(
            testsCount,
            activeAttempts,
            completedCurrent.Count,
            completedPrevious.Count,
            averageScoreCurrent,
            averageScorePrevious,
            assessmentCurrent.Count,
            assessmentPrevious.Count,
            averageAssessmentDurationCurrent,
            averageAssessmentDurationPrevious);
    }

    private async Task<ContentMetricsDto> BuildContentMetricsAsync(
        DateTimeOffset currentPeriodStart,
        DateTimeOffset currentPeriodEnd,
        DateTimeOffset previousPeriodStart,
        DateTimeOffset previousPeriodEnd,
        DateOnly currentWeekStart,
        DateOnly previousWeekStart,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var blogsQuery = _dbContext.Blogs
            .AsNoTracking()
            .Where(blog => !blog.IsDeleted);

        var publishedBlogs = await blogsQuery
            .Where(blog => blog.Status == BlogStatus.Published)
            .CountAsync(cancellationToken);

        var draftBlogs = await blogsQuery
            .Where(blog => blog.Status == BlogStatus.Draft)
            .CountAsync(cancellationToken);

        var blogViewsCurrent = await _dbContext.BlogViews
            .AsNoTracking()
            .Where(view => view.ViewDate >= currentWeekStart && view.ViewDate <= today)
            .CountAsync(cancellationToken);

        var blogViewsPrevious = await _dbContext.BlogViews
            .AsNoTracking()
            .Where(view => view.ViewDate >= previousWeekStart && view.ViewDate < currentWeekStart)
            .CountAsync(cancellationToken);

        var blogCommentsCurrent = await _dbContext.BlogComments
            .AsNoTracking()
            .Where(comment => !comment.IsDeleted &&
                              comment.CreateDate >= currentPeriodStart &&
                              comment.CreateDate <= currentPeriodEnd)
            .CountAsync(cancellationToken);

        var blogCommentsPrevious = await _dbContext.BlogComments
            .AsNoTracking()
            .Where(comment => !comment.IsDeleted &&
                              comment.CreateDate >= previousPeriodStart &&
                              comment.CreateDate < previousPeriodEnd)
            .CountAsync(cancellationToken);

        var productCommentsCurrent = await _dbContext.ProductComments
            .AsNoTracking()
            .Where(comment => !comment.IsDeleted &&
                              comment.CreateDate >= currentPeriodStart &&
                              comment.CreateDate <= currentPeriodEnd)
            .CountAsync(cancellationToken);

        var productCommentsPrevious = await _dbContext.ProductComments
            .AsNoTracking()
            .Where(comment => !comment.IsDeleted &&
                              comment.CreateDate >= previousPeriodStart &&
                              comment.CreateDate < previousPeriodEnd)
            .CountAsync(cancellationToken);

        var productsQuery = _dbContext.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted);

        var publishedProducts = await productsQuery
            .Where(product => product.IsPublished)
            .CountAsync(cancellationToken);

        var newProductsCurrent = await productsQuery
            .Where(product => product.CreateDate >= currentPeriodStart && product.CreateDate <= currentPeriodEnd)
            .CountAsync(cancellationToken);

        var newProductsPrevious = await productsQuery
            .Where(product => product.CreateDate >= previousPeriodStart && product.CreateDate < previousPeriodEnd)
            .CountAsync(cancellationToken);

        return new ContentMetricsDto(
            publishedBlogs,
            draftBlogs,
            blogViewsCurrent,
            blogViewsPrevious,
            blogCommentsCurrent,
            blogCommentsPrevious,
            productCommentsCurrent,
            productCommentsPrevious,
            publishedProducts,
            newProductsCurrent,
            newProductsPrevious);
    }

    private static decimal CalculateAverageScore(IReadOnlyCollection<AttemptScoreProjection> items)
    {
        if (items.Count == 0)
        {
            return 0m;
        }

        var scores = items
            .Where(item => item.ScorePercentage.HasValue)
            .Select(item => item.ScorePercentage!.Value)
            .ToArray();

        if (scores.Length == 0)
        {
            return 0m;
        }

        var average = scores.Average();
        return decimal.Round(average, 2, MidpointRounding.AwayFromZero);
    }

    private static double CalculateAverageAssessmentDurationMinutes(IReadOnlyCollection<AssessmentDurationProjection> items)
    {
        if (items.Count == 0)
        {
            return 0d;
        }

        var durations = items
            .Select(item => Math.Max(0d, item.DurationMinutes))
            .ToArray();

        if (durations.Length == 0)
        {
            return 0d;
        }

        return Math.Round(durations.Average(), 1, MidpointRounding.AwayFromZero);
    }

    private sealed record AttemptScoreProjection(DateTimeOffset CompletedAt, decimal? ScorePercentage);

    private sealed record AssessmentDurationProjection(DateTime StartedAt, DateTime CompletedAt)
    {
        public double DurationMinutes => (CompletedAt - StartedAt).TotalMinutes;
    }
}
