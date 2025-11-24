using EndPoint.WebSite.Areas.Organization.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Areas.Organization.Controllers;

[Area("Organization")]
[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "داشبورد سازمانی";
        ViewData["GreetingTitle"] = "داشبورد سازمانی";
        ViewData["GreetingSubtitle"] = "نمای کلی از عملکرد سازمان شما";

        var viewModel = new OrganizationDashboardViewModel
        {
            TotalUsers = 150,
            ActiveTests = 25,
            CompletedTests = 120,
            TopWeaknesses = new[]
            {
                new WeaknessSummary { Type = "A1", Description = "کمبود اعتماد به نفس", Count = 45 },
                new WeaknessSummary { Type = "B2", Description = "مشکل در تصمیم‌گیری", Count = 32 },
                new WeaknessSummary { Type = "C3", Description = "کمبود مهارت ارتباطی", Count = 28 }
            }
        };

        return View(viewModel);
    }
}
