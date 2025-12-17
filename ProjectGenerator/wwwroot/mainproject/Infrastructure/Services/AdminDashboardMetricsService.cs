using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs.Dashboard;
using Attar.Application.Interfaces;
using Attar.Domain.Entities;
using Attar.Domain.Enums;
using Attar.Infrastructure.Persistence;
using Attar.SharedKernel.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Attar.Infrastructure.Services;

public sealed class AdminDashboardMetricsService : IAdminDashboardMetricsService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationLogRepository _applicationLogRepository;

    public AdminDashboardMetricsService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IApplicationLogRepository applicationLogRepository)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _applicationLogRepository = applicationLogRepository;
    }

    public async Task<SystemPerformanceSummaryDto> GetSummaryAsync(DateTimeOffset referenceTime, CancellationToken cancellationToken)
    {
        var now = referenceTime;
        var persianCalendar = new PersianCalendar();
        
        // Calculate periods based on Persian calendar months
        var currentPeriodEnd = now.GetPersianMonthEnd();
        var currentPeriodStart = now.GetPersianMonthStart();
        
        // Previous period is the previous month (using Persian calendar)
        var localNow = now.ToLocalTime();
        var currentYear = persianCalendar.GetYear(localNow.DateTime);
        var currentMonth = persianCalendar.GetMonth(localNow.DateTime);
        
        // Calculate previous month
        var previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
        var previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;
        var previousMonthFirstDay = persianCalendar.ToDateTime(previousYear, previousMonth, 1, 0, 0, 0, 0);
        var previousMonthDays = persianCalendar.GetDaysInMonth(previousYear, previousMonth);
        var previousMonthLastDay = persianCalendar.ToDateTime(previousYear, previousMonth, previousMonthDays, 23, 59, 59, 999);
        
        var previousPeriodStart = new DateTimeOffset(previousMonthFirstDay, TimeSpan.Zero);
        var previousPeriodEnd = new DateTimeOffset(previousMonthLastDay, TimeSpan.Zero);

        var currentPeriodStartUtc = currentPeriodStart.UtcDateTime;
        var currentPeriodEndUtc = currentPeriodEnd.UtcDateTime;
        var previousPeriodStartUtc = previousPeriodStart.UtcDateTime;
        var previousPeriodEndUtc = previousPeriodEnd.UtcDateTime;

        // Calculate week based on Persian calendar (week starts on Saturday)
        var todayDateOnly = DateOnly.FromDateTime(now.ToLocalTime().Date);
        var currentWeekStart = todayDateOnly.GetPersianWeekStart();
        var previousWeekStart = currentWeekStart.AddDays(-7);

        var people = await BuildPeopleMetricsAsync(currentPeriodStart, currentPeriodEnd, previousPeriodStart, previousPeriodEnd, cancellationToken);
        var commerce = await BuildCommerceMetricsAsync(currentPeriodStart, currentPeriodEnd, previousPeriodStart, previousPeriodEnd, cancellationToken);
        var content = await BuildContentMetricsAsync(currentPeriodStart, currentPeriodEnd, previousPeriodStart, previousPeriodEnd, currentWeekStart, previousWeekStart, todayDateOnly, cancellationToken);
        var pages = await BuildPagesMetricsAsync(cancellationToken);
        var visits = await BuildVisitsMetricsAsync(currentPeriodStart, currentPeriodEnd, previousPeriodStart, previousPeriodEnd, cancellationToken);

        var period = new PeriodWindowDto(
            currentPeriodStart,
            currentPeriodEnd,
            previousPeriodStart,
            previousPeriodEnd,
            currentWeekStart,
            previousWeekStart);

        // Additional quick counts for admin dashboard cards
        var pendingProductRequestsCount = await _dbContext.ProductRequests
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == Domain.Enums.ProductRequestStatus.Pending)
            .CountAsync(cancellationToken);

        var newProductViolationReportsCount = await _dbContext.ProductViolationReports
            .AsNoTracking()
            .Where(r => !r.IsDeleted && !r.IsReviewed)
            .CountAsync(cancellationToken);

        var pendingProductCommentsCount = await _dbContext.ProductComments
            .AsNoTracking()
            .Where(c => !c.IsDeleted && !c.IsApproved)
            .CountAsync(cancellationToken);

        // Today error logs count (from LogsDbContext via repository)
        var todayStartUtc = new DateTimeOffset(todayDateOnly.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var todayEndUtc = todayStartUtc.AddDays(1).AddTicks(-1);
        var todayErrorLogsCount = await _applicationLogRepository.GetApplicationLogsCountAsync(
            level: "ERROR",
            fromDate: todayStartUtc,
            toDate: todayEndUtc,
            sourceContext: null,
            applicationName: null,
            machineName: null,
            environment: null,
            cancellationToken);

        return new SystemPerformanceSummaryDto(
            period,
            people,
            commerce,
            content,
            pages,
            visits,
            pendingProductRequestsCount,
            newProductViolationReportsCount,
            pendingProductCommentsCount,
            todayErrorLogsCount,
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

        var sellersQuery = _dbContext.SellerProfiles
            .AsNoTracking()
            .Where(seller => !seller.IsDeleted);

        var totalSellers = await sellersQuery.CountAsync(cancellationToken);
        var activeSellers = await sellersQuery.Where(seller => seller.IsActive).CountAsync(cancellationToken);
        var newSellersCurrent = await sellersQuery
            .Where(seller => seller.CreateDate >= currentPeriodStart && seller.CreateDate <= currentPeriodEnd)
            .CountAsync(cancellationToken);
        var newSellersPrevious = await sellersQuery
            .Where(seller => seller.CreateDate >= previousPeriodStart && seller.CreateDate < previousPeriodEnd)
            .CountAsync(cancellationToken);

        return new PeopleMetricsDto(
            totalUsers,
            activeUsers,
            newUsersCurrent,
            newUsersPrevious,
            totalSellers,
            activeSellers,
            newSellersCurrent,
            newSellersPrevious);
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

    private async Task<PagesMetricsDto> BuildPagesMetricsAsync(CancellationToken cancellationToken)
    {
        var pagesQuery = _dbContext.Pages.AsNoTracking();

        var totalPages = await pagesQuery.CountAsync(cancellationToken);
        var publishedPages = await pagesQuery.Where(page => page.IsPublished).CountAsync(cancellationToken);
        var draftPages = totalPages - publishedPages;
        var totalPageViews = await pagesQuery.SumAsync(page => (int?)page.ViewCount ?? 0, cancellationToken);
        var footerPages = await pagesQuery.Where(page => page.IsPublished && page.ShowInFooter).CountAsync(cancellationToken);
        var quickAccessPages = await pagesQuery.Where(page => page.IsPublished && page.ShowInQuickAccess).CountAsync(cancellationToken);

        return new PagesMetricsDto(
            totalPages,
            publishedPages,
            draftPages,
            totalPageViews,
            footerPages,
            quickAccessPages);
    }

    private async Task<VisitsMetricsDto> BuildVisitsMetricsAsync(
        DateTimeOffset currentPeriodStart,
        DateTimeOffset currentPeriodEnd,
        DateTimeOffset previousPeriodStart,
        DateTimeOffset previousPeriodEnd,
        CancellationToken cancellationToken)
    {
        var currentPeriodStartDate = DateOnly.FromDateTime(currentPeriodStart.Date);
        var currentPeriodEndDate = DateOnly.FromDateTime(currentPeriodEnd.Date);
        var previousPeriodStartDate = DateOnly.FromDateTime(previousPeriodStart.Date);
        var previousPeriodEndDate = DateOnly.FromDateTime(previousPeriodEnd.Date);

        // Get site visit statistics for all time
        var allSiteVisits = await _dbContext.SiteVisits
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalSiteVisits = allSiteVisits.Count;
        var uniqueVisitors = allSiteVisits.Select(v => v.ViewerIp.ToString()).Distinct().Count();

        // Get site visits for current period
        var siteVisitsCurrent = allSiteVisits
            .Where(v => v.VisitDate >= currentPeriodStartDate && v.VisitDate <= currentPeriodEndDate)
            .Count();

        // Get site visits for previous period
        var siteVisitsPrevious = allSiteVisits
            .Where(v => v.VisitDate >= previousPeriodStartDate && v.VisitDate < previousPeriodEndDate)
            .Count();

        // Get page visits for current period
        var pageVisitsCurrent = await _dbContext.PageVisits
            .AsNoTracking()
            .Where(v => v.VisitDate >= currentPeriodStartDate && v.VisitDate <= currentPeriodEndDate)
            .CountAsync(cancellationToken);

        // Get page visits for previous period
        var pageVisitsPrevious = await _dbContext.PageVisits
            .AsNoTracking()
            .Where(v => v.VisitDate >= previousPeriodStartDate && v.VisitDate < previousPeriodEndDate)
            .CountAsync(cancellationToken);

        return new VisitsMetricsDto(
            totalSiteVisits,
            uniqueVisitors,
            siteVisitsCurrent,
            siteVisitsPrevious,
            pageVisitsCurrent,
            pageVisitsPrevious);
    }
}
