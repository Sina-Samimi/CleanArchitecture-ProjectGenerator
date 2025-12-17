using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Attar.WebSite.Controllers;

/// <summary>
/// Shows a friendly maintenance page when the site is in maintenance mode.
/// </summary>
public sealed class MaintenanceController : Controller
{
    private readonly IConfiguration _configuration;

    public MaintenanceController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    [Route("/Maintenance")]
    public IActionResult Index()
    {
        var message = _configuration["MaintenanceMode:Message"]
                      ?? "سایت در حال تعمیر و به\u200cروزرسانی است. لطفاً دقایقی دیگر دوباره تلاش کنید.";

        var estimatedTime = _configuration["MaintenanceMode:EstimatedTime"];

        ViewData["Title"] = "سایت در دست تعمیر";
        ViewData["Message"] = message;
        ViewData["EstimatedTime"] = estimatedTime;

        return View();
    }
}


