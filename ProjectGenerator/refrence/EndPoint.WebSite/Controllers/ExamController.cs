using System.Globalization;
using System.Security.Claims;
using Arsis.Domain.Entities.Assessments;
using Arsis.Infrastructure.Persistence;
using EndPoint.WebSite.Growth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EndPoint.WebSite.Controllers;

[Authorize]
public sealed class ExamController : Controller
{
    private readonly AppDbContext _dbContext;

    public ExamController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("exam/start")]
    public async Task<IActionResult> Start(CancellationToken cancellationToken)
    {
        var cliftonCount = await _dbContext.AssessmentQuestions.CountAsync(q => q.TestType == AssessmentTestType.Clifton, cancellationToken);
        var pvqCount = await _dbContext.AssessmentQuestions.CountAsync(q => q.TestType == AssessmentTestType.Pvq, cancellationToken);

        if (cliftonCount == 0 || pvqCount == 0)
        {
            TempData["Error"] = "سؤالات آزمون هنوز بارگذاری نشده‌اند.";
            return RedirectToAction("Index", "Home");
        }

        var userId = ResolveUserId();
        var run = new AssessmentRun
        {
            UserId = userId,
            StartedAt = DateTime.UtcNow
        };

        await _dbContext.AssessmentRuns.AddAsync(run, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Clifton), new { index = 1, runId = run.Id });
    }

    [HttpGet("exam/clifton/{index:int}")]
    public async Task<IActionResult> Clifton(int index, int runId, CancellationToken cancellationToken)
    {
        if (!await EnsureRunOwnedByUser(runId, cancellationToken))
        {
            TempData["Error"] = "دسترسی به این آزمون مجاز نیست.";
            return RedirectToAction(nameof(Start));
        }

        var question = await _dbContext.AssessmentQuestions
            .Where(q => q.TestType == AssessmentTestType.Clifton && q.Index == index)
            .SingleOrDefaultAsync(cancellationToken);

        if (question is null)
        {
            return RedirectToAction(nameof(Pvq), new { index = 1, runId });
        }

        var viewModel = new CliftonQuestionVm
        {
            RunId = runId,
            Index = index,
            Total = await _dbContext.AssessmentQuestions.CountAsync(q => q.TestType == AssessmentTestType.Clifton, cancellationToken),
            TextA = question.TextA ?? string.Empty,
            TextB = question.TextB ?? string.Empty
        };

        return View(viewModel);
    }

    [HttpPost("exam/clifton/{index:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clifton(int index, int runId, string answer, CancellationToken cancellationToken)
    {
        if (!await EnsureRunOwnedByUser(runId, cancellationToken))
        {
            TempData["Error"] = "دسترسی به این آزمون مجاز نیست.";
            return RedirectToAction(nameof(Start));
        }

        answer = (answer ?? string.Empty).Trim().ToUpperInvariant();
        if (answer is not "A" and not "B")
        {
            TempData["Error"] = "لطفاً یکی از گزینه‌های A یا B را انتخاب کنید.";
            return RedirectToAction(nameof(Clifton), new { index, runId });
        }

        var question = await _dbContext.AssessmentQuestions
            .Where(q => q.TestType == AssessmentTestType.Clifton && q.Index == index)
            .SingleOrDefaultAsync(cancellationToken);

        if (question is null)
        {
            return RedirectToAction(nameof(Pvq), new { index = 1, runId });
        }

        await UpsertResponseAsync(runId, question.Id, answer, cancellationToken);

        var total = await _dbContext.AssessmentQuestions.CountAsync(q => q.TestType == AssessmentTestType.Clifton, cancellationToken);
        var nextIndex = index + 1;
        if (nextIndex > total)
        {
            return RedirectToAction(nameof(Pvq), new { index = 1, runId });
        }

        return RedirectToAction(nameof(Clifton), new { index = nextIndex, runId });
    }

    [HttpGet("exam/pvq/{index:int}")]
    public async Task<IActionResult> Pvq(int index, int runId, CancellationToken cancellationToken)
    {
        if (!await EnsureRunOwnedByUser(runId, cancellationToken))
        {
            TempData["Error"] = "دسترسی به این آزمون مجاز نیست.";
            return RedirectToAction(nameof(Start));
        }

        var question = await _dbContext.AssessmentQuestions
            .Where(q => q.TestType == AssessmentTestType.Pvq && q.Index == index)
            .SingleOrDefaultAsync(cancellationToken);

        if (question is null)
        {
            return RedirectToAction(nameof(Finish), new { runId });
        }

        var viewModel = new PvqQuestionVm
        {
            RunId = runId,
            Index = index,
            Total = await _dbContext.AssessmentQuestions.CountAsync(q => q.TestType == AssessmentTestType.Pvq, cancellationToken),
            Text = question.Text ?? string.Empty
        };

        return View(viewModel);
    }

    [HttpPost("exam/pvq/{index:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pvq(int index, int runId, string answer, CancellationToken cancellationToken)
    {
        if (!await EnsureRunOwnedByUser(runId, cancellationToken))
        {
            TempData["Error"] = "دسترسی به این آزمون مجاز نیست.";
            return RedirectToAction(nameof(Start));
        }

        if (!int.TryParse(answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric) || numeric is < 1 or > 6)
        {
            TempData["Error"] = "لطفاً عددی بین ۱ تا ۶ انتخاب کنید.";
            return RedirectToAction(nameof(Pvq), new { index, runId });
        }

        var question = await _dbContext.AssessmentQuestions
            .Where(q => q.TestType == AssessmentTestType.Pvq && q.Index == index)
            .SingleOrDefaultAsync(cancellationToken);

        if (question is null)
        {
            return RedirectToAction(nameof(Finish), new { runId });
        }

        await UpsertResponseAsync(runId, question.Id, numeric.ToString(CultureInfo.InvariantCulture), cancellationToken);

        var total = await _dbContext.AssessmentQuestions.CountAsync(q => q.TestType == AssessmentTestType.Pvq, cancellationToken);
        var nextIndex = index + 1;
        if (nextIndex > total)
        {
            return RedirectToAction(nameof(Finish), new { runId });
        }

        return RedirectToAction(nameof(Pvq), new { index = nextIndex, runId });
    }

    [HttpGet("exam/finish")]
    public async Task<IActionResult> Finish(int runId, [FromServices] AssessmentService assessmentService, CancellationToken cancellationToken)
    {
        if (!await EnsureRunOwnedByUser(runId, cancellationToken))
        {
            TempData["Error"] = "دسترسی به این آزمون مجاز نیست.";
            return RedirectToAction(nameof(Start));
        }

        await assessmentService.EvaluateAsync(runId, cancellationToken);
        return RedirectToAction("Show", "Result", new { runId });
    }

    private async Task UpsertResponseAsync(int runId, int questionId, string answer, CancellationToken cancellationToken)
    {
        var response = await _dbContext.AssessmentResponses
            .SingleOrDefaultAsync(r => r.AssessmentRunId == runId && r.AssessmentQuestionId == questionId, cancellationToken);

        if (response is null)
        {
            response = new AssessmentUserResponse
            {
                AssessmentRunId = runId,
                AssessmentQuestionId = questionId,
                Answer = answer,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.AssessmentResponses.AddAsync(response, cancellationToken);
        }
        else
        {
            response.Answer = answer;
            response.CreatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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

    private async Task<bool> EnsureRunOwnedByUser(int runId, CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        return await _dbContext.AssessmentRuns.AnyAsync(r => r.Id == runId && r.UserId == userId, cancellationToken);
    }
}

public sealed class CliftonQuestionVm
{
    public int RunId { get; set; }

    public int Index { get; set; }

    public int Total { get; set; }

    public string TextA { get; set; } = string.Empty;

    public string TextB { get; set; } = string.Empty;
}

public sealed class PvqQuestionVm
{
    public int RunId { get; set; }

    public int Index { get; set; }

    public int Total { get; set; }

    public string Text { get; set; } = string.Empty;
}
