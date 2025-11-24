using System.Text.Json;
using Arsis.Application.Assessments;
using Arsis.Application.Interfaces;
using EndPoint.WebSite.Areas.Admin.Models.Assessments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class AssessmentController : Controller
{
    private readonly IAssessmentService _assessmentService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AssessmentController> _logger;

    public AssessmentController(IAssessmentService assessmentService, IWebHostEnvironment environment, ILogger<AssessmentController> logger)
    {
        _assessmentService = assessmentService;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await BuildViewModelAsync(new AssessmentCalculationViewModel
        {
            RequestJson = await LoadSampleAsync()
        }, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AssessmentCalculationViewModel model, [FromForm] string? jobGroup, [FromForm] bool includeDebug, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var request = JsonSerializer.Deserialize<AssessmentRequest>(model.RequestJson, options);
            if (request is null)
            {
                model.Error = "ساختار JSON نامعتبر است.";
                return View(await BuildViewModelAsync(model, cancellationToken));
            }

            if (includeDebug)
            {
                model.DebugResult = await _assessmentService.EvaluateWithDebugAsync(request, jobGroup, cancellationToken);
                model.Result = new AssessmentResponse(model.DebugResult.Scores, model.DebugResult.JobGroupsFinal, model.DebugResult.Plans);
            }
            else
            {
                model.Result = await _assessmentService.EvaluateAsync(request, cancellationToken);
                model.DebugResult = null;
            }
        }
        catch (JsonException ex)
        {
            model.Error = "فرمت JSON معتبر نیست.";
            _logger.LogWarning(ex, "Failed to parse assessment JSON payload.");
        }
        catch (Exception ex)
        {
            model.Error = "محاسبه با خطا مواجه شد.";
            _logger.LogError(ex, "Assessment calculation failed in admin panel.");
        }

        return View(await BuildViewModelAsync(model, cancellationToken));
    }

    private async Task<AssessmentCalculationViewModel> BuildViewModelAsync(AssessmentCalculationViewModel model, CancellationToken cancellationToken)
    {
        var overview = await _assessmentService.GetMatricesOverviewAsync(cancellationToken);
        model.Matrices = overview.Matrices
            .Select(m => new AssessmentMatrixViewModel
            {
                Name = m.Name,
                Rows = m.Rows,
                Columns = m.Columns,
                LoadedAt = m.LoadedAt,
                SourcePath = m.SourcePath
            })
            .ToList();

        if (string.IsNullOrWhiteSpace(model.RequestJson))
        {
            model.RequestJson = await LoadSampleAsync();
        }

        return model;
    }

    private async Task<string> LoadSampleAsync()
    {
        try
        {
            var samplePath = Path.Combine(_environment.ContentRootPath, "samples", "request.sample.json");
            if (System.IO.File.Exists(samplePath))
            {
                return await System.IO.File.ReadAllTextAsync(samplePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to load sample request JSON.");
        }

        return "{\n  \"userId\": 1,\n  \"inventoryId\": \"demo-001\"\n}";
    }
}
