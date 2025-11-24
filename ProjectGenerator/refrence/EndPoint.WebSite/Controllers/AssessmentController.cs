using EndPoint.WebSite.Growth;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Controllers;

[Route("assessment")]
public sealed class AssessmentController : Controller
{
    private readonly AssessmentService _assessmentService;

    public AssessmentController(AssessmentService assessmentService)
    {
        _assessmentService = assessmentService;
    }

    [HttpGet("{runId:int}")]
    public async Task<IActionResult> Show(int runId, CancellationToken cancellationToken)
    {
        var model = await _assessmentService.EvaluateAsync(runId, cancellationToken);
        ViewData["RunId"] = runId;
        return View("~/Views/Result/Show.cshtml", model);
    }
}
