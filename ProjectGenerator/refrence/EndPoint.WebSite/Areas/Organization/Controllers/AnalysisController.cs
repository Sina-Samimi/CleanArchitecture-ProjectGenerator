using Arsis.Application.DTOs.OrganizationAnalysis;
using Arsis.Application.Interfaces;
using Arsis.Application.Queries.OrganizationAnalysis;
using EndPoint.WebSite.Areas.Organization.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EndPoint.WebSite.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
public class AnalysisController : Controller
{
    private readonly IMediator _mediator;

    public AnalysisController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "تحلیل سازمانی";
        ViewData["GreetingTitle"] = "تحلیل سازمانی";
        ViewData["GreetingSubtitle"] = "بررسی عملکرد و ضعف‌های سازمان شما";

        // For now, we'll use a placeholder organization ID
        // In a real implementation, this would come from user claims
        var organizationId = Guid.NewGuid(); // Placeholder

        try
        {
            // Get organization weaknesses
            var weaknessesQuery = new GetOrganizationWeaknessesQuery(organizationId);
            var weaknessesResult = await _mediator.Send(weaknessesQuery);

            // Get user test results for different job groups
            var jobGroups = new[] { "G1", "G2", "G3", "G4", "G5" };
            var userTestResults = new List<UserTestResultDto>();

            foreach (var jobGroup in jobGroups)
            {
                var testResultsQuery = new GetUserTestResultsQuery(organizationId, jobGroup);
                var testResultsResult = await _mediator.Send(testResultsQuery);
                if (testResultsResult.IsSuccess)
                {
                    userTestResults.AddRange(testResultsResult.Value);
                }
            }

            var viewModel = new OrganizationAnalysisViewModel
            {
                OrganizationWeaknesses = weaknessesResult.IsSuccess ? weaknessesResult.Value : new List<OrganizationWeaknessDto>(),
                UserTestResults = userTestResults,
                AvailableWeaknessTypes = GetAvailableWeaknessTypes()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"خطا در بارگذاری تحلیل سازمانی: {ex.Message}";
            return View(new OrganizationAnalysisViewModel());
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetWeakUsers(string weaknessType)
    {
        // For now, we'll use a placeholder organization ID
        var organizationId = Guid.NewGuid(); // Placeholder

        try
        {
            var query = new GetWeakUsersQuery(organizationId, weaknessType);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                return Json(new { success = false, error = result.Error });
            }

            return Json(new { success = true, data = result.Value });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    private IEnumerable<SelectListItem> GetAvailableWeaknessTypes()
    {
        return new[]
        {
            new SelectListItem { Value = "A1", Text = "کمبود اعتماد به نفس" },
            new SelectListItem { Value = "A2", Text = "مشکل در تصمیم‌گیری" },
            new SelectListItem { Value = "B1", Text = "کمبود مهارت ارتباطی" },
            new SelectListItem { Value = "B2", Text = "مشکل در مدیریت زمان" },
            new SelectListItem { Value = "C1", Text = "کمبود مهارت رهبری" },
            new SelectListItem { Value = "C2", Text = "مشکل در حل مسئله" }
        };
    }
}
