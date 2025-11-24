using System.Linq;
using System.Security.Claims;
using Arsis.Infrastructure.Persistence;
using EndPoint.WebSite.App;
using EndPoint.WebSite.Growth;
using EndPoint.WebSite.Models.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;

namespace EndPoint.WebSite.Controllers;

[Authorize]
public sealed class ResultController : Controller
{
    private readonly AssessmentService _assessmentService;
    private readonly AppDbContext _dbContext;
    private readonly IJobGroupLabelsProvider _labelsProvider;
    private readonly IWebHostEnvironment _environment;

    public ResultController(
        AssessmentService assessmentService,
        AppDbContext dbContext,
        IJobGroupLabelsProvider labelsProvider,
        IWebHostEnvironment environment)
    {
        _assessmentService = assessmentService;
        _dbContext = dbContext;
        _labelsProvider = labelsProvider;
        _environment = environment;
    }

    [HttpGet("result/{runId:int}")]
    public async Task<IActionResult> Show(int runId, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var ownsRun = await _dbContext.AssessmentRuns.AnyAsync(r => r.Id == runId && r.UserId == userId, cancellationToken);
        if (!ownsRun)
        {
            TempData["Error"] = "دسترسی به نتیجه این آزمون مجاز نیست.";
            return RedirectToAction("Index", "Home");
        }

        var assessmentResult = await _assessmentService.EvaluateAsync(runId, cancellationToken);
        var labels = await _labelsProvider.LoadAsync(cancellationToken);

        var vm = new ResultViewModel
        {
            RunId = runId,
            PvqScores = assessmentResult.PvqScores
                .OrderByDescending(x => x.Value)
                .Select(x => new ScoreItemVm
                {
                    Code = x.Key,
                    Label = x.Key,
                    Score = x.Value
                })
                .ToList(),
            CliftonScores = assessmentResult.CliftonScores
                .OrderByDescending(x => x.Value)
                .Select(x => new ScoreItemVm
                {
                    Code = x.Key,
                    Label = x.Key,
                    Score = x.Value
                })
                .ToList(),
            JobGroups = assessmentResult.JobScores
                .OrderByDescending(x => x.Score)
                .Select(x => new JobGroupScoreVm
                {
                    Code = x.JobCode,
                    Label = labels.TryGetValue(x.JobCode, out var label) ? label : x.JobCode,
                    Score = x.Score
                })
                .ToList(),
            TopPlans = assessmentResult.SkillPlans
                .Take(3)
                .Select(plan => new JobSkillPlanVm
                {
                    JobGroup = labels.TryGetValue(plan.JobCode, out var label) ? label : plan.JobCode,
                    SG1 = plan.SelfAwareness.Take(5).ToList(),
                    SG2 = plan.SelfBuilding.Take(5).ToList(),
                    SG3 = plan.SelfDevelopment.Take(5).ToList(),
                    SG4 = plan.SelfActualization.Take(5).ToList()
                })
                .ToList()
        };

        return View(vm);
    }

    [HttpGet("result/{runId:int}/pdf")]
    public async Task<IActionResult> Pdf(int runId, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var ownsRun = await _dbContext.AssessmentRuns.AnyAsync(r => r.Id == runId && r.UserId == userId, cancellationToken);
        if (!ownsRun)
        {
            TempData["Error"] = "دسترسی به نتیجه این آزمون مجاز نیست.";
            return RedirectToAction("Index", "Home");
        }

        var assessmentResult = await _assessmentService.EvaluateAsync(runId, cancellationToken);
        var labels = await _labelsProvider.LoadAsync(cancellationToken);
        var fontPath = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "fonts", "Vazirmatn-Regular.ttf");
        var pdfBytes = ResultPdfBuilder.Build(assessmentResult, labels, fontPath).GeneratePdf();

        return File(pdfBytes, "application/pdf", $"assessment-{runId}.pdf");
    }

    private int ResolveUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdValue, out var numericId))
        {
            return numericId;
        }

        return Math.Abs(userIdValue?.GetHashCode() ?? Environment.TickCount);
    }
}
