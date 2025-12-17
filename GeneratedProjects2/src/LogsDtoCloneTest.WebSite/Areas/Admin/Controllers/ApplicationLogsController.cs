using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Queries.Logs;
using LogsDtoCloneTest.WebSite.Areas.Admin.Models;
using LogsDtoCloneTest.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class ApplicationLogsController : Controller
{
    private const int DefaultPageSize = 20;
    private readonly IMediator _mediator;

    public ApplicationLogsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? level,
        string? fromDatePersian,
        string? toDatePersian,
        string? sourceContext,
        string? applicationName,
        string? machineName,
        string? environment,
        int page = 1,
        int pageSize = 20)
    {
        ViewData["Title"] = "مانیتور لاگ‌ها";
        ViewData["Subtitle"] = "نمایش و جستجوی لاگ‌های سیستم";

        var filter = new ApplicationLogFilterViewModel
        {
            Level = level,
            FromDatePersian = fromDatePersian,
            ToDatePersian = toDatePersian,
            SourceContext = sourceContext,
            ApplicationName = applicationName,
            MachineName = machineName,
            Environment = environment,
            Page = page,
            PageSize = pageSize
        };

        var cancellationToken = HttpContext.RequestAborted;

        // Parse dates from Persian format
        DateTimeOffset? fromDate = null;
        DateTimeOffset? toDate = null;

        if (!string.IsNullOrWhiteSpace(filter.FromDatePersian))
        {
            fromDate = UserFilterFormatting.ParsePersianDate(filter.FromDatePersian, toExclusiveEnd: false, out _);
        }

        if (!string.IsNullOrWhiteSpace(filter.ToDatePersian))
        {
            toDate = UserFilterFormatting.ParsePersianDate(filter.ToDatePersian, toExclusiveEnd: true, out _);
        }

        var pageNumber = filter.Page > 0 ? filter.Page : 1;
        var finalPageSize = filter.PageSize > 0 && filter.PageSize <= 100 ? filter.PageSize : DefaultPageSize;

        filter.PageSize = finalPageSize;

        var query = new GetApplicationLogsQuery(
            Level: filter.Level,
            FromDate: fromDate,
            ToDate: toDate,
            SourceContext: filter.SourceContext,
            ApplicationName: filter.ApplicationName,
            MachineName: filter.MachineName,
            Environment: filter.Environment,
            PageNumber: pageNumber,
            PageSize: finalPageSize);

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "خطا در دریافت لاگ‌ها.";
            return View(new ApplicationLogListViewModel
            {
                Logs = Array.Empty<ApplicationLogListItemViewModel>(),
                PageNumber = pageNumber,
                PageSize = finalPageSize,
                TotalCount = 0,
                TotalPages = 0,
                Filter = filter
            });
        }

        var data = result.Value;

        var viewModel = new ApplicationLogListViewModel
        {
            Logs = data.Items.Select(log => new ApplicationLogListItemViewModel
            {
                Id = log.Id,
                Level = log.Level,
                Message = log.Message,
                Exception = log.Exception,
                SourceContext = log.SourceContext,
                RequestPath = log.RequestPath,
                RequestMethod = log.RequestMethod,
                StatusCode = log.StatusCode,
                ElapsedMs = log.ElapsedMs,
                UserAgent = log.UserAgent,
                RemoteIpAddress = log.RemoteIpAddress,
                ApplicationName = log.ApplicationName,
                MachineName = log.MachineName,
                Environment = log.Environment,
                CreateDate = log.CreateDate
            }).ToList(),
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            TotalCount = data.TotalCount,
            TotalPages = data.TotalPages,
            Filter = filter
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        ViewData["Title"] = "جزئیات لاگ";
        ViewData["Subtitle"] = "مشاهده جزئیات کامل لاگ";

        var cancellationToken = HttpContext.RequestAborted;
        var query = new GetApplicationLogDetailsQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "لاگ یافت نشد.";
            return RedirectToAction("Index");
        }

        var log = result.Value;

        var viewModel = new ApplicationLogDetailsViewModel
        {
            Id = log.Id,
            Level = log.Level,
            Message = log.Message,
            Exception = log.Exception,
            SourceContext = log.SourceContext,
            Properties = log.Properties,
            RequestPath = log.RequestPath,
            RequestMethod = log.RequestMethod,
            StatusCode = log.StatusCode,
            ElapsedMs = log.ElapsedMs,
            UserAgent = log.UserAgent,
            RemoteIpAddress = log.RemoteIpAddress,
            ApplicationName = log.ApplicationName,
            MachineName = log.MachineName,
            Environment = log.Environment,
            CreateDate = log.CreateDate,
            UpdateDate = log.UpdateDate
        };

        return View(viewModel);
    }
}

