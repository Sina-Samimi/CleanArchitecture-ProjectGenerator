using System;

namespace Arsis.Application.DTOs.Dashboard;

public sealed record SystemPerformanceSummaryDto(
    PeriodWindowDto Period,
    PeopleMetricsDto People,
    CommerceMetricsDto Commerce,
    LearningMetricsDto Learning,
    ContentMetricsDto Content,
    DateTimeOffset GeneratedAt);

public sealed record PeriodWindowDto(
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd,
    DateTimeOffset PreviousPeriodStart,
    DateTimeOffset PreviousPeriodEnd,
    DateOnly CurrentWeekStart,
    DateOnly PreviousWeekStart);

public sealed record PeopleMetricsDto(
    int TotalUsers,
    int ActiveUsers,
    int NewUsersCurrentPeriod,
    int NewUsersPreviousPeriod,
    int TotalTeachers,
    int ActiveTeachers,
    int NewTeachersCurrentPeriod,
    int NewTeachersPreviousPeriod);

public sealed record CommerceMetricsDto(
    decimal TotalRevenueAllTime,
    decimal RevenueCurrentPeriod,
    decimal RevenuePreviousPeriod,
    decimal AverageOrderValueCurrentPeriod,
    decimal AverageOrderValuePreviousPeriod,
    int SuccessfulTransactionsCurrentPeriod,
    int SuccessfulTransactionsPreviousPeriod,
    int FailedTransactionsCurrentPeriod,
    int PendingInvoices,
    int OverdueInvoices);

public sealed record LearningMetricsDto(
    int TotalTests,
    int ActiveTestAttempts,
    int CompletedAttemptsCurrentPeriod,
    int CompletedAttemptsPreviousPeriod,
    decimal AverageScoreCurrentPeriod,
    decimal AverageScorePreviousPeriod,
    int AssessmentCompletionsCurrentPeriod,
    int AssessmentCompletionsPreviousPeriod,
    double AverageAssessmentDurationMinutesCurrentPeriod,
    double AverageAssessmentDurationMinutesPreviousPeriod);

public sealed record ContentMetricsDto(
    int PublishedBlogs,
    int DraftBlogs,
    int BlogViewsCurrentWeek,
    int BlogViewsPreviousWeek,
    int BlogCommentsCurrentPeriod,
    int BlogCommentsPreviousPeriod,
    int ProductCommentsCurrentPeriod,
    int ProductCommentsPreviousPeriod,
    int PublishedProducts,
    int NewProductsCurrentPeriod,
    int NewProductsPreviousPeriod);
