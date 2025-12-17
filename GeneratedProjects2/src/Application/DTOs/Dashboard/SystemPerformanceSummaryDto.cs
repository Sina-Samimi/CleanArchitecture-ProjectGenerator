using System;

namespace LogsDtoCloneTest.Application.DTOs.Dashboard;

public sealed record SystemPerformanceSummaryDto(
    PeriodWindowDto Period,
    PeopleMetricsDto People,
    CommerceMetricsDto Commerce,
    ContentMetricsDto Content,
    PagesMetricsDto Pages,
    VisitsMetricsDto Visits,
    int PendingProductRequestsCount,
    int NewProductViolationReportsCount,
    int PendingProductCommentsCount,
    int TodayErrorLogsCount,
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
    int TotalSellers,
    int ActiveSellers,
    int NewSellersCurrentPeriod,
    int NewSellersPreviousPeriod);

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

public sealed record PagesMetricsDto(
    int TotalPages,
    int PublishedPages,
    int DraftPages,
    int TotalPageViews,
    int FooterPages,
    int QuickAccessPages);

public sealed record VisitsMetricsDto(
    int TotalSiteVisits,
    int UniqueVisitors,
    int SiteVisitsCurrentPeriod,
    int SiteVisitsPreviousPeriod,
    int PageVisitsCurrentPeriod,
    int PageVisitsPreviousPeriod);
