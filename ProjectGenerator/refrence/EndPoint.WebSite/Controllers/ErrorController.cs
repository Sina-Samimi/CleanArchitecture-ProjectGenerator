using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Controllers;

public sealed class ErrorController : Controller
{
    [Route("Error/403")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["Title"] = "دسترسی غیرمجاز";
        return View("403");
    }

    [Route("Error/500")]
    public IActionResult ServerError()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        ViewData["Title"] = "بروز خطای داخلی";
        return View("500");
    }
}
