using System;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LogTableRenameTest.Application.DTOs.Dashboard;
using LogTableRenameTest.SharedKernel.Extensions;

namespace LogTableRenameTest.WebSite.Areas.Admin.Models.Dashboard;

public sealed class SystemPerformanceSummaryViewModel
{
    private static readonly CultureInfo PersianCulture = CultureInfo.GetCultureInfo("fa-IR");

    private SystemPerformanceSummaryViewModel(
        DateTimeOffset generatedAt,
        PeriodWindowViewModel period,
        PeopleMetricsViewModel people,
        CommerceMetricsViewModel commerce,
        ContentMetricsViewModel content,
        PagesMetricsViewModel pages,
        VisitsMetricsViewModel visits,
        IReadOnlyList<AlertViewModel> alerts,
        int pendingProductRequestsCount,
        int newProductViolationReportsCount,
        int pendingProductCommentsCount,
        int todayErrorLogsCount)
    {
        GeneratedAt = generatedAt;
        Period = period;
        People = people;
        Commerce = commerce;
        Content = content;
        Pages = pages;
        Visits = visits;
        PendingProductRequestsCount = pendingProductRequestsCount;
        NewProductViolationReportsCount = newProductViolationReportsCount;
        PendingProductCommentsCount = pendingProductCommentsCount;
        TodayErrorLogsCount = todayErrorLogsCount;
        Alerts = alerts;

        ActiveUserRate = people.TotalUsers == 0
            ? 0m
            : Math.Round((people.ActiveUsers / (decimal)people.TotalUsers) * 100m, 1, MidpointRounding.AwayFromZero);

        NewUsersTrendPercent = CalculateTrendPercentage(people.NewUsersCurrentPeriod, people.NewUsersPreviousPeriod);
        NewSellersTrendPercent = CalculateTrendPercentage(people.NewSellersCurrentPeriod, people.NewSellersPreviousPeriod);

        RevenueTrendPercent = CalculateTrendPercentage(commerce.RevenueCurrentPeriod, commerce.RevenuePreviousPeriod);
        AverageOrderValueTrendPercent = CalculateTrendPercentage(commerce.AverageOrderValueCurrentPeriod, commerce.AverageOrderValuePreviousPeriod);

        BlogViewTrendPercent = CalculateTrendPercentage(content.BlogViewsCurrentWeek, content.BlogViewsPreviousWeek);
        BlogCommentTrendPercent = CalculateTrendPercentage(content.BlogCommentsCurrentPeriod, content.BlogCommentsPreviousPeriod);
        ProductCommentTrendPercent = CalculateTrendPercentage(content.ProductCommentsCurrentPeriod, content.ProductCommentsPreviousPeriod);
        NewProductsTrendPercent = CalculateTrendPercentage(content.NewProductsCurrentPeriod, content.NewProductsPreviousPeriod);

        SiteVisitsTrendPercent = CalculateTrendPercentage(visits.SiteVisitsCurrentPeriod, visits.SiteVisitsPreviousPeriod);
        PageVisitsTrendPercent = CalculateTrendPercentage(visits.PageVisitsCurrentPeriod, visits.PageVisitsPreviousPeriod);

        CurrentPeriodLabel = $"{period.CurrentPeriodStart.ToPersianDateString()} تا {period.CurrentPeriodEnd.ToPersianDateString()}";
        PreviousPeriodLabel = $"{period.PreviousPeriodStart.ToPersianDateString()} تا {period.PreviousPeriodEnd.ToPersianDateString()}";
        CurrentWeekLabel = "بازدید تا امروز";
        PreviousWeekLabel = $"{period.PreviousWeekStart.ToDateTime(TimeOnly.MinValue).ToPersianDateString()} تا {period.CurrentWeekStart.AddDays(-1).ToDateTime(TimeOnly.MinValue).ToPersianDateString()}";
    }

    public DateTimeOffset GeneratedAt { get; }

    public PeriodWindowViewModel Period { get; }

    public PeopleMetricsViewModel People { get; }

    public CommerceMetricsViewModel Commerce { get; }

    public ContentMetricsViewModel Content { get; }

    public PagesMetricsViewModel Pages { get; }

    public VisitsMetricsViewModel Visits { get; }

    public IReadOnlyList<AlertViewModel> Alerts { get; }

    public int PendingProductRequestsCount { get; }

    public int NewProductViolationReportsCount { get; }

    public int PendingProductCommentsCount { get; }

    public int TodayErrorLogsCount { get; }

    public decimal ActiveUserRate { get; }

    public decimal NewUsersTrendPercent { get; }

    public decimal NewSellersTrendPercent { get; }

    public decimal RevenueTrendPercent { get; }

    public decimal AverageOrderValueTrendPercent { get; }

    public decimal BlogViewTrendPercent { get; }

    public decimal BlogCommentTrendPercent { get; }

    public decimal ProductCommentTrendPercent { get; }

    public decimal NewProductsTrendPercent { get; }

    public decimal SiteVisitsTrendPercent { get; }

    public decimal PageVisitsTrendPercent { get; }

    public string CurrentPeriodLabel { get; }

    public string PreviousPeriodLabel { get; }

    public string CurrentWeekLabel { get; }

    public string PreviousWeekLabel { get; }

    public static string FormatInt(int value) => value.ToString("N0", PersianCulture);

    public static string FormatDecimal(decimal value, string format = "N1") => value.ToString(format, PersianCulture);

    public static string FormatDouble(double value, string format = "N1") => value.ToString(format, PersianCulture);

    public static string FormatCurrency(decimal value) => value.ToString("N0", PersianCulture);

    public static string FormatCurrencyCompact(decimal amount)
    {
        var abs = Math.Abs(amount);
        if (abs >= 1_000_000_000m)
        {
            return $"{FormatDecimal(amount / 1_000_000_000m, "N2")} میلیارد تومان";
        }

        if (abs >= 1_000_000m)
        {
            return $"{FormatDecimal(amount / 1_000_000m, "N1")} میلیون تومان";
        }

        if (abs >= 1_000m)
        {
            return $"{FormatDecimal(amount / 1_000m, "N1")} هزار تومان";
        }

        return $"{FormatCurrency(amount)} تومان";
    }

    public static string GetTrendLabel(decimal percent)
    {
        if (percent > 0m)
        {
            return $"{FormatDecimal(percent)}٪ رشد";
        }

        if (percent < 0m)
        {
            return $"{FormatDecimal(Math.Abs(percent))}٪ افت";
        }

        return "بدون تغییر";
    }

    public static string GetTrendCssClass(decimal percent)
    {
        if (percent > 5m)
        {
            return "trend-up";
        }

        if (percent < -5m)
        {
            return "trend-down";
        }

        return "trend-flat";
    }

    public static string GetTrendIcon(decimal percent)
    {
        if (percent > 0m)
        {
            return "bi-arrow-up";
        }

        if (percent < 0m)
        {
            return "bi-arrow-down";
        }

        return "bi-arrow-right";
    }

    public static decimal ClampPercent(decimal value)
    {
        if (value < 0m)
        {
            return 0m;
        }

        if (value > 100m)
        {
            return 100m;
        }

        return Math.Round(value, 1, MidpointRounding.AwayFromZero);
    }

    public static SystemPerformanceSummaryViewModel FromDto(SystemPerformanceSummaryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var period = new PeriodWindowViewModel(
            dto.Period.CurrentPeriodStart,
            dto.Period.CurrentPeriodEnd,
            dto.Period.PreviousPeriodStart,
            dto.Period.PreviousPeriodEnd,
            dto.Period.CurrentWeekStart,
            dto.Period.PreviousWeekStart);

        var people = new PeopleMetricsViewModel(
            dto.People.TotalUsers,
            dto.People.ActiveUsers,
            dto.People.NewUsersCurrentPeriod,
            dto.People.NewUsersPreviousPeriod,
            dto.People.TotalSellers,
            dto.People.ActiveSellers,
            dto.People.NewSellersCurrentPeriod,
            dto.People.NewSellersPreviousPeriod);

        var commerce = new CommerceMetricsViewModel(
            dto.Commerce.TotalRevenueAllTime,
            dto.Commerce.RevenueCurrentPeriod,
            dto.Commerce.RevenuePreviousPeriod,
            dto.Commerce.AverageOrderValueCurrentPeriod,
            dto.Commerce.AverageOrderValuePreviousPeriod,
            dto.Commerce.SuccessfulTransactionsCurrentPeriod,
            dto.Commerce.SuccessfulTransactionsPreviousPeriod,
            dto.Commerce.FailedTransactionsCurrentPeriod,
            dto.Commerce.PendingInvoices,
            dto.Commerce.OverdueInvoices);

        var content = new ContentMetricsViewModel(
            dto.Content.PublishedBlogs,
            dto.Content.DraftBlogs,
            dto.Content.BlogViewsCurrentWeek,
            dto.Content.BlogViewsPreviousWeek,
            dto.Content.BlogCommentsCurrentPeriod,
            dto.Content.BlogCommentsPreviousPeriod,
            dto.Content.ProductCommentsCurrentPeriod,
            dto.Content.ProductCommentsPreviousPeriod,
            dto.Content.PublishedProducts,
            dto.Content.NewProductsCurrentPeriod,
            dto.Content.NewProductsPreviousPeriod);

        var pages = new PagesMetricsViewModel(
            dto.Pages.TotalPages,
            dto.Pages.PublishedPages,
            dto.Pages.DraftPages,
            dto.Pages.TotalPageViews,
            dto.Pages.FooterPages,
            dto.Pages.QuickAccessPages);

        var visits = new VisitsMetricsViewModel(
            dto.Visits.TotalSiteVisits,
            dto.Visits.UniqueVisitors,
            dto.Visits.SiteVisitsCurrentPeriod,
            dto.Visits.SiteVisitsPreviousPeriod,
            dto.Visits.PageVisitsCurrentPeriod,
            dto.Visits.PageVisitsPreviousPeriod);

        var alerts = BuildAlerts(commerce, content, visits, dto);

        return new SystemPerformanceSummaryViewModel(
            dto.GeneratedAt,
            period,
            people,
            commerce,
            content,
            pages,
            visits,
            alerts,
            dto.PendingProductRequestsCount,
            dto.NewProductViolationReportsCount,
            dto.PendingProductCommentsCount,
            dto.TodayErrorLogsCount);
    }

    private static IReadOnlyList<AlertViewModel> BuildAlerts(
        CommerceMetricsViewModel commerce,
        ContentMetricsViewModel content,
        VisitsMetricsViewModel visits,
        SystemPerformanceSummaryDto dto)
    {
        var items = new List<AlertViewModel>();

        if (commerce.OverdueInvoices > 0)
        {
            items.Add(new AlertViewModel(
                "bi-exclamation-octagon",
                "صورتحساب‌های معوق",
                $"{FormatInt(commerce.OverdueInvoices)} فقره در انتظار تسویه است.",
                "danger"));
        }

        var siteVisitsTrend = CalculateTrendPercentage(visits.SiteVisitsCurrentPeriod, visits.SiteVisitsPreviousPeriod);
        if (siteVisitsTrend < -10m)
        {
            items.Add(new AlertViewModel(
                "bi-graph-down-arrow",
                "کاهش بازدید سایت",
                $"{GetTrendLabel(siteVisitsTrend)} در بازدیدهای سایت.",
                "warning"));
        }

        var blogViewTrend = CalculateTrendPercentage(content.BlogViewsCurrentWeek, content.BlogViewsPreviousWeek);
        if (blogViewTrend < -10m)
        {
            items.Add(new AlertViewModel(
                "bi-lightning-charge",
                "تعامل محتوایی کاهش یافته",
                $"{GetTrendLabel(blogViewTrend)} در بازدید هفتگی وبلاگ.",
                "warning"));
        }

        if (items.Count == 0)
        {
            items.Add(new AlertViewModel(
                "bi-check-circle",
                "همه‌چیز تحت کنترل است",
                "شاخص‌های کلیدی در محدوده هدف قرار دارند.",
                "success"));
        }

        return items;
    }

    private static decimal CalculateTrendPercentage(decimal current, decimal previous)
    {
        if (previous == 0m)
        {
            return current == 0m ? 0m : 100m;
        }

        var delta = ((current - previous) / Math.Abs(previous)) * 100m;
        return Math.Round(delta, 1, MidpointRounding.AwayFromZero);
    }
}

public sealed record PeriodWindowViewModel(
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd,
    DateTimeOffset PreviousPeriodStart,
    DateTimeOffset PreviousPeriodEnd,
    DateOnly CurrentWeekStart,
    DateOnly PreviousWeekStart);

public sealed record PeopleMetricsViewModel(
    int TotalUsers,
    int ActiveUsers,
    int NewUsersCurrentPeriod,
    int NewUsersPreviousPeriod,
    int TotalSellers,
    int ActiveSellers,
    int NewSellersCurrentPeriod,
    int NewSellersPreviousPeriod);

public sealed record CommerceMetricsViewModel(
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

public sealed record ContentMetricsViewModel(
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

public sealed record PagesMetricsViewModel(
    int TotalPages,
    int PublishedPages,
    int DraftPages,
    int TotalPageViews,
    int FooterPages,
    int QuickAccessPages);

public sealed record VisitsMetricsViewModel(
    int TotalSiteVisits,
    int UniqueVisitors,
    int SiteVisitsCurrentPeriod,
    int SiteVisitsPreviousPeriod,
    int PageVisitsCurrentPeriod,
    int PageVisitsPreviousPeriod);

public sealed record AlertViewModel(string Icon, string Title, string Description, string Tone);
