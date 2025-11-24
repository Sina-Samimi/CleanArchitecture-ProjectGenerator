using System.Collections.Generic;
using Arsis.Application.DTOs.Tests;
using Arsis.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EndPoint.WebSite.Areas.Admin.Models.Tests;

public sealed class TestIndexViewModel
{
    public List<TestListDto> Tests { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public TestType? Type { get; set; }
    public TestStatus? Status { get; set; }
    public SelectList TestTypes { get; set; } = null!;
    public SelectList TestStatuses { get; set; } = null!;
    public TestStatistics Statistics { get; set; } = new();
}

public sealed class TestStatistics
{
    public int TotalTests { get; set; }
    public int PublishedTests { get; set; }
    public int DraftTests { get; set; }
    public int ArchivedTests { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal HighestPrice { get; set; }
    public decimal LowestPrice { get; set; }
    public int TotalParticipants { get; set; }
    public int TotalQuestions { get; set; }
}

public sealed class TestIndexRequest
{
    public string? Search { get; set; }
    public TestType? Type { get; set; }
    public TestStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
