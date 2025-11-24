using System;
using System.Collections.Generic;
using Arsis.Application.DTOs.Tests;
using Arsis.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EndPoint.WebSite.Areas.Admin.Models.Tests;

public sealed class TestAttemptsViewModel
{
    public List<UserTestAttemptDto> Attempts { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public Guid? TestId { get; set; }
    public string? UserId { get; set; }
    public TestAttemptStatus? Status { get; set; }
    public string? Search { get; set; }
    public DateTimeOffset? StartedFrom { get; set; }
    public DateTimeOffset? StartedTo { get; set; }
    public TestAttemptStatisticsViewModel Statistics { get; set; } = new();
    public SelectList? Tests { get; set; }
    public SelectList Statuses { get; set; } = null!;
    public IReadOnlyList<int> PageSizeOptions { get; set; } = new[] { 10, 20, 50, 100 };
}

public sealed class TestAttemptStatisticsViewModel
{
    public int TotalAttempts { get; set; }
    public int CompletedAttempts { get; set; }
    public int InProgressAttempts { get; set; }
    public int CancelledAttempts { get; set; }
    public int ExpiredAttempts { get; set; }
    public int UniqueParticipants { get; set; }
    public decimal? AverageScore { get; set; }
    public double? AverageCompletionMinutes { get; set; }
    public DateTimeOffset? FirstAttemptAt { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }

    public double CompletionRate => TotalAttempts == 0
        ? 0
        : Math.Round((double)CompletedAttempts / TotalAttempts * 100, 1);

    public double AbandonRate => TotalAttempts == 0
        ? 0
        : Math.Round((double)(CancelledAttempts + ExpiredAttempts) / TotalAttempts * 100, 1);
}

public sealed class TestAttemptsRequest
{
    public Guid? TestId { get; set; }
    public string? UserId { get; set; }
    public TestAttemptStatus? Status { get; set; }
    public string? Search { get; set; }
    public DateTimeOffset? StartedFrom { get; set; }
    public DateTimeOffset? StartedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
