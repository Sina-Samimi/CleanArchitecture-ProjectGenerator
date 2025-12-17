using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed class ApplicationLogListViewModel
{
    public IReadOnlyCollection<ApplicationLogListItemViewModel> Logs { get; init; } = Array.Empty<ApplicationLogListItemViewModel>();
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public ApplicationLogFilterViewModel Filter { get; init; } = new();
}

public sealed class ApplicationLogListItemViewModel
{
    public Guid Id { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Exception { get; init; }
    public string? SourceContext { get; init; }
    public string? RequestPath { get; init; }
    public string? RequestMethod { get; init; }
    public int? StatusCode { get; init; }
    public double? ElapsedMs { get; init; }
    public string? UserAgent { get; init; }
    public string? RemoteIpAddress { get; init; }
    public string? ApplicationName { get; init; }
    public string? MachineName { get; init; }
    public string? Environment { get; init; }
    public DateTimeOffset CreateDate { get; init; }
}

public sealed class ApplicationLogFilterViewModel
{
    [Display(Name = "سطح")]
    public string? Level { get; set; }

    [Display(Name = "از تاریخ")]
    public string? FromDatePersian { get; set; }

    [Display(Name = "تا تاریخ")]
    public string? ToDatePersian { get; set; }

    [Display(Name = "Source Context")]
    public string? SourceContext { get; set; }

    [Display(Name = "نام برنامه")]
    public string? ApplicationName { get; set; }

    [Display(Name = "نام ماشین")]
    public string? MachineName { get; set; }

    [Display(Name = "محیط")]
    public string? Environment { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public bool HasActiveFilters => 
        !string.IsNullOrWhiteSpace(Level) ||
        !string.IsNullOrWhiteSpace(FromDatePersian) ||
        !string.IsNullOrWhiteSpace(ToDatePersian) ||
        !string.IsNullOrWhiteSpace(SourceContext) ||
        !string.IsNullOrWhiteSpace(ApplicationName) ||
        !string.IsNullOrWhiteSpace(MachineName) ||
        !string.IsNullOrWhiteSpace(Environment);
}

public sealed class ApplicationLogDetailsViewModel
{
    public Guid Id { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Exception { get; init; }
    public string? SourceContext { get; init; }
    public string? Properties { get; init; }
    public string? RequestPath { get; init; }
    public string? RequestMethod { get; init; }
    public int? StatusCode { get; init; }
    public double? ElapsedMs { get; init; }
    public string? UserAgent { get; init; }
    public string? RemoteIpAddress { get; init; }
    public string? ApplicationName { get; init; }
    public string? MachineName { get; init; }
    public string? Environment { get; init; }
    public DateTimeOffset CreateDate { get; init; }
    public DateTimeOffset UpdateDate { get; init; }
}

