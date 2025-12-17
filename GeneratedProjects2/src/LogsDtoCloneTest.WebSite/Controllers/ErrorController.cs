using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.Controllers;

public sealed class ErrorController : Controller
{
    [Route("Error/403")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["Title"] = "دسترسی غیرمجاز";
        
        // Check if user is in User area
        var area = RouteData.Values["area"]?.ToString();
        if (string.Equals(area, "User", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Areas/User/Views/Error/403.cshtml");
        }
        
        return View("403");
    }

    [Route("Error/404")]
    public IActionResult NotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        ViewData["Title"] = "صفحه یافت نشد";
        
        // Check if user is in User area
        var area = RouteData.Values["area"]?.ToString();
        if (string.Equals(area, "User", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Areas/User/Views/Error/404.cshtml");
        }
        
        return View("404");
    }

    [Route("Error/500")]
    public IActionResult ServerError()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        ViewData["Title"] = "بروز خطای داخلی";
        
        // Check if user is in User area
        var area = RouteData.Values["area"]?.ToString();
        if (string.Equals(area, "User", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Areas/User/Views/Error/500.cshtml");
        }
        
        return View("500");
    }
}
