using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;
using Arsis.Application.Commands.Tests;
using Arsis.Application.Assessments;
using Arsis.Application.DTOs.Tests;
using Arsis.Application.Interfaces;
using Arsis.Application.Queries.Tests;
using Arsis.Domain.Enums;
using EndPoint.WebSite.App;
using EndPoint.WebSite.Growth;
using EndPoint.WebSite.Models.Test;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;

namespace EndPoint.WebSite.Controllers;

public sealed class TestController : Controller
{
    private readonly IMediator _mediator;
    private readonly ISiteCategoryRepository _categoryRepository;
    private readonly ILogger<TestController> _logger;
    private readonly IJobGroupLabelsProvider _jobGroupLabelsProvider;
    private readonly IWebHostEnvironment _environment;
    private static readonly JsonSerializerOptions AssessmentSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TestController(
        IMediator mediator, 
        ISiteCategoryRepository categoryRepository,
        ILogger<TestController> logger,
        IJobGroupLabelsProvider jobGroupLabelsProvider,
        IWebHostEnvironment environment)
    {
        _mediator = mediator;
        _categoryRepository = categoryRepository;
        _logger = logger;
        _jobGroupLabelsProvider = jobGroupLabelsProvider;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, TestType? type, Guid? categoryId, bool? isFree, int page = 1)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var query = new GetPublicTestListQuery(
            search,
            type,
            categoryId,
            isFree,
            page,
            12);

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return View(new TestListViewModel());
        }

        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var categoryOptions = categories
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == categoryId
            })
            .ToList();
        categoryOptions.Insert(0, new SelectListItem { Value = "", Text = "همه دسته‌بندی‌ها", Selected = !categoryId.HasValue });

        var testTypeOptions = Enum.GetValues<TestType>()
            .Select(t => new SelectListItem
            {
                Value = ((int)t).ToString(),
                Text = GetTestTypeName(t),
                Selected = t == type
            })
            .ToList();
        testTypeOptions.Insert(0, new SelectListItem { Value = "", Text = "همه انواع", Selected = !type.HasValue });

        var viewModel = new TestListViewModel
        {
            Tests = result.Value.Items,
            TotalCount = result.Value.TotalCount,
            Page = result.Value.Page,
            PageSize = result.Value.PageSize,
            TotalPages = result.Value.TotalPages,
            Search = search,
            Type = type,
            CategoryId = categoryId,
            IsFree = isFree,
            Categories = new SelectList(categoryOptions, "Value", "Text", categoryId?.ToString() ?? ""),
            TestTypes = new SelectList(testTypeOptions, "Value", "Text", type?.ToString() ?? "")
        };

        return View(viewModel);
    }

    private static string GetTestTypeName(TestType type) => type switch
    {
        TestType.General => "تست عمومی",
        TestType.Disc => "تست DISC",
        TestType.Clifton => "تست کلیفتون",
        TestType.CliftonSchwartz => "تست کلیفتون + شوارتز",
        TestType.Raven => "تست هوش ریون",
        TestType.Personality => "تست شخصیت‌شناسی",
        _ => type.ToString()
    };

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _mediator.Send(new GetTestByIdQuery(id, userId), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        var test = result.Value;

        // For paid tests, allow multiple attempts - CanUserAttempt check is done in StartTestAttemptCommand
        // For free tests, check CanUserAttempt
        var canStart = test.IsAvailable && User.Identity?.IsAuthenticated == true;
        if (test.Price == 0)
        {
            canStart = canStart && test.CanUserAttempt;
        }

        var viewModel = new TestDetailsViewModel
        {
            Test = test,
            CanStart = canStart
        };

        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(Guid id, Guid? invoiceId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("PhoneLogin", "Account");
        }

        var command = new StartTestAttemptCommand(id, userId, invoiceId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        return RedirectToAction(nameof(Take), new { attemptId = result.Value });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Take(Guid attemptId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _mediator.Send(new GetUserTestAttemptQuery(attemptId), cancellationToken);

        if (!result.IsSuccess || result.Value.UserId != userId)
        {
            TempData["Error"] = "آزمون مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var attempt = result.Value;

        if (attempt.Status != Arsis.Domain.Enums.TestAttemptStatus.InProgress)
        {
            return RedirectToAction(nameof(Result), new { attemptId });
        }

        var testResult = await _mediator.Send(new GetTestByIdQuery(attempt.TestId), cancellationToken);
        if (!testResult.IsSuccess)
        {
            TempData["Error"] = "اطلاعات تست یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var test = testResult.Value;

        var viewModel = new TakeTestViewModel
        {
            Attempt = attempt,
            Test = test,
            CurrentQuestionIndex = 0
        };

        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAnswer(SubmitAnswerViewModel model)
    {
        var cancellationToken = HttpContext.RequestAborted;

        _logger.LogInformation("🌐 SubmitAnswer request received: AttemptId={AttemptId}, QuestionId={QuestionId}, SelectedOptionId={SelectedOptionId}, LikertValue={LikertValue}, TextAnswer={TextAnswer}", 
            model.AttemptId, model.QuestionId, model.SelectedOptionId, model.LikertValue, model.TextAnswer?.Substring(0, Math.Min(50, model.TextAnswer?.Length ?? 0)));

        var command = new SubmitTestAnswerCommand(
            model.AttemptId,
            model.QuestionId,
            model.SelectedOptionId,
            model.TextAnswer,
            model.LikertValue);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("❌ SubmitAnswer failed: {Error}", result.Error);
            return Json(new { success = false, error = result.Error });
        }

        _logger.LogInformation("✅ SubmitAnswer successful");
        return Json(new { success = true });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(Guid attemptId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Verify ownership
        var attemptResult = await _mediator.Send(new GetUserTestAttemptQuery(attemptId), cancellationToken);
        if (!attemptResult.IsSuccess || attemptResult.Value.UserId != userId)
        {
            TempData["Error"] = "دسترسی غیرمجاز.";
            return RedirectToAction(nameof(Index));
        }

        var command = new CompleteTestAttemptCommand(attemptId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Take), new { attemptId });
        }

        TempData["Success"] = "آزمون با موفقیت تکمیل شد.";
        return RedirectToAction(nameof(Result), new { attemptId });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Result(Guid attemptId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _mediator.Send(new GetUserTestAttemptQuery(attemptId), cancellationToken);

        if (!result.IsSuccess || result.Value.UserId != userId)
        {
            TempData["Error"] = "نتیجه مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Index));
        }

        var attempt = result.Value;

        if (attempt.Status == Arsis.Domain.Enums.TestAttemptStatus.InProgress)
        {
            return RedirectToAction(nameof(Take), new { attemptId });
        }

        var labels = await _jobGroupLabelsProvider.LoadAsync(cancellationToken);
        var computation = ComputeAssessmentResult(attempt, labels);
        TestResultDto? errorResult = attempt.Results.FirstOrDefault(r => r.ResultType == "EvaluationError");

        if (!string.IsNullOrEmpty(computation.ErrorMessage) && !TempData.ContainsKey("Error"))
        {
            TempData["Error"] = computation.ErrorMessage;
        }

        var viewModel = new TestResultViewModel
        {
            Attempt = attempt,
            CliftonScores = computation.CliftonScores,
            PvqScores = computation.PvqScores,
            JobGroups = computation.JobGroups,
            TopPlans = computation.Plans,
            ErrorResult = errorResult
        };

        return View(viewModel);
    }

    [Authorize]
    [HttpGet("test/result/{attemptId:guid}/pdf")]
    public async Task<IActionResult> ResultPdf(Guid attemptId)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _mediator.Send(new GetUserTestAttemptQuery(attemptId), cancellationToken);

        if (!result.IsSuccess || result.Value.UserId != userId)
        {
            TempData["Error"] = "نتیجه مورد نظر یافت نشد.";
            return RedirectToAction(nameof(Result), new { attemptId });
        }

        var attempt = result.Value;

        if (attempt.Status == Arsis.Domain.Enums.TestAttemptStatus.InProgress)
        {
            TempData["Error"] = "ابتدا آزمون را تکمیل کنید.";
            return RedirectToAction(nameof(Take), new { attemptId });
        }

        var labels = await _jobGroupLabelsProvider.LoadAsync(cancellationToken);
        var computation = ComputeAssessmentResult(attempt, labels);

        if (!string.IsNullOrEmpty(computation.ErrorMessage))
        {
            TempData["Error"] = computation.ErrorMessage;
        }

        if (computation.AssessmentDto is null)
        {
            TempData["Error"] ??= "داده‌ای برای تولید فایل PDF در دسترس نیست.";
            return RedirectToAction(nameof(Result), new { attemptId });
        }

        var fontRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var fontPath = Path.Combine(fontRoot, "fonts", "Vazirmatn-Regular.ttf");
        var pdfBytes = ResultPdfBuilder.Build(computation.AssessmentDto, labels, fontPath).GeneratePdf();

        return File(pdfBytes, "application/pdf", $"test-result-{attemptId}.pdf");
    }

    private AssessmentComputation ComputeAssessmentResult(UserTestAttemptDetailDto attempt, IReadOnlyDictionary<string, string> labels)
    {
        var data = new AssessmentComputation();

        if (attempt.TestType != TestType.CliftonSchwartz)
        {
            return data;
        }

        var summaryResult = attempt.Results
            .FirstOrDefault(r => r.ResultType == "CliftonSchwartzResponse" && !string.IsNullOrWhiteSpace(r.AdditionalData));

        if (summaryResult is null)
        {
            return data;
        }

        try
        {
            var assessment = JsonSerializer.Deserialize<AssessmentResponse>(summaryResult.AdditionalData!, AssessmentSerializerOptions);
            if (assessment is null)
            {
                return data;
            }

            var cliftonList = assessment.Scores.Clifton
                .Select(kvp => new ScoreItemVm
                {
                    Code = kvp.Key,
                    Label = AssessmentLabelResolver.ResolveClifton(kvp.Key),
                    Score = Math.Round(kvp.Value, 6, MidpointRounding.AwayFromZero)
                })
                .OrderByDescending(s => s.Score)
                .ToList();
            data.CliftonScores.AddRange(cliftonList);

            var pvqList = assessment.Scores.Pvq
                .Select(kvp => new ScoreItemVm
                {
                    Code = kvp.Key,
                    Label = AssessmentLabelResolver.ResolvePvq(kvp.Key),
                    Score = Math.Round(kvp.Value, 6, MidpointRounding.AwayFromZero)
                })
                .OrderByDescending(s => s.Score)
                .ToList();
            data.PvqScores.AddRange(pvqList);

            var jobList = assessment.JobGroups
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => new JobGroupScoreVm
                {
                    Code = kvp.Key,
                    Label = labels.TryGetValue(kvp.Key, out var label) ? label : kvp.Key,
                    Score = Math.Round(kvp.Value, 6, MidpointRounding.AwayFromZero)
                })
                .ToList();
            data.JobGroups.AddRange(jobList);

            var planList = assessment.Plans
                .Select(plan => new JobSkillPlanVm
                {
                    JobGroup = labels.TryGetValue(plan.JobGroup, out var label) ? label : plan.JobGroup,
                    SG1 = plan.Top5SelfAwareness.Select(AssessmentLabelResolver.ResolveSkill).ToList(),
                    SG2 = plan.Top5SelfBuilding.Select(AssessmentLabelResolver.ResolveSkill).ToList(),
                    SG3 = plan.Top5SelfDevelopment.Select(AssessmentLabelResolver.ResolveSkill).ToList(),
                    SG4 = plan.Top5SelfActualization.Select(AssessmentLabelResolver.ResolveSkill).ToList()
                })
                .ToList();
            data.Plans.AddRange(planList);

            var cliftonDict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in assessment.Scores.Clifton)
            {
                var label = AssessmentLabelResolver.ResolveClifton(kvp.Key);
                cliftonDict[label] = Math.Round(kvp.Value, 6, MidpointRounding.AwayFromZero);
            }

            var pvqDict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in assessment.Scores.Pvq)
            {
                var label = AssessmentLabelResolver.ResolvePvq(kvp.Key);
                pvqDict[label] = Math.Round(kvp.Value, 6, MidpointRounding.AwayFromZero);
            }

            var jobScores = assessment.JobGroups
                .Select(kvp =>
                {
                    var label = labels.TryGetValue(kvp.Key, out var fa) ? fa : kvp.Key;
                    return new JobScoreDto(label, Math.Round(kvp.Value, 6, MidpointRounding.AwayFromZero));
                })
                .ToList();

            var planDtos = assessment.Plans
                .Select(plan => new SkillPlanDto(
                    labels.TryGetValue(plan.JobGroup, out var label) ? label : plan.JobGroup,
                    plan.Top5SelfAwareness.Select(AssessmentLabelResolver.ResolveSkill).ToList(),
                    plan.Top5SelfBuilding.Select(AssessmentLabelResolver.ResolveSkill).ToList(),
                    plan.Top5SelfDevelopment.Select(AssessmentLabelResolver.ResolveSkill).ToList(),
                    plan.Top5SelfActualization.Select(AssessmentLabelResolver.ResolveSkill).ToList()))
                .ToList();

            data.AssessmentDto = new AssessmentResultDto(cliftonDict, pvqDict, jobScores, planDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize assessment response for attempt {AttemptId}", attempt.Id);
            data.ErrorMessage = "خطا در پردازش نتایج تحلیل.";
        }

        return data;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyTests()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("PhoneLogin", "Account");
        }

        var result = await _mediator.Send(new GetUserTestAttemptsQuery(userId), cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return View(new MyTestsViewModel());
        }

        var viewModel = new MyTestsViewModel
        {
            Attempts = result.Value
        };

        return View(viewModel);
    }

    private sealed class AssessmentComputation
    {
        public List<ScoreItemVm> CliftonScores { get; } = new();
        public List<ScoreItemVm> PvqScores { get; } = new();
        public List<JobGroupScoreVm> JobGroups { get; } = new();
        public List<JobSkillPlanVm> Plans { get; } = new();
        public AssessmentResultDto? AssessmentDto { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
