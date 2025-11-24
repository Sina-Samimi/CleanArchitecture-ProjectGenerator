using System;
using System.Collections.Generic;
using Arsis.Application.DTOs.Tests;
using Arsis.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EndPoint.WebSite.Models.Test;

public sealed class TestListViewModel
{
    public List<TestListDto> Tests { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public TestType? Type { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsFree { get; set; }
    public SelectList Categories { get; set; } = new SelectList(new List<object>());
    public SelectList TestTypes { get; set; } = new SelectList(new List<object>());
}

public sealed class TestDetailsViewModel
{
    public TestDetailDto Test { get; set; } = null!;
    public bool CanStart { get; set; }
}

public sealed class TakeTestViewModel
{
    public UserTestAttemptDetailDto Attempt { get; set; } = null!;
    public TestDetailDto Test { get; set; } = null!;
    public int CurrentQuestionIndex { get; set; }
}

public sealed class SubmitAnswerViewModel
{
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public string? TextAnswer { get; set; }
    public int? LikertValue { get; set; }
}

public sealed class ScoreItemVm
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public double Score { get; init; }
}

public sealed class JobGroupScoreVm
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public double Score { get; init; }
}

public sealed class JobSkillPlanVm
{
    public string JobGroup { get; init; } = string.Empty;
    public IReadOnlyList<string> SG1 { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SG2 { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SG3 { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SG4 { get; init; } = Array.Empty<string>();
}

public sealed class TestResultViewModel
{
    public UserTestAttemptDetailDto Attempt { get; init; } = null!;
    public IReadOnlyList<ScoreItemVm> CliftonScores { get; init; } = Array.Empty<ScoreItemVm>();
    public IReadOnlyList<ScoreItemVm> PvqScores { get; init; } = Array.Empty<ScoreItemVm>();
    public IReadOnlyList<JobGroupScoreVm> JobGroups { get; init; } = Array.Empty<JobGroupScoreVm>();
    public IReadOnlyList<JobSkillPlanVm> TopPlans { get; init; } = Array.Empty<JobSkillPlanVm>();
    public TestResultDto? ErrorResult { get; init; }
}

public sealed class MyTestsViewModel
{
    public List<UserTestAttemptDto> Attempts { get; set; } = new();
    public List<PurchasedTestViewModel> PurchasedTests { get; set; } = new();
}

public sealed class PurchasedTestViewModel
{
    public Guid TestId { get; set; }
    public string TestTitle { get; set; } = null!;
    public TestType TestType { get; set; }
    public DateTimeOffset PurchaseDate { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public UserTestAttemptDto? Attempt { get; set; }
}
