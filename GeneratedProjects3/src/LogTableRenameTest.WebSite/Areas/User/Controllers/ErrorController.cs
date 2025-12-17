using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LogTableRenameTest.SharedKernel.Authorization;

namespace LogTableRenameTest.WebSite.Areas.User.Controllers;

[Area("User")]
[Authorize]
public sealed class ErrorController : Controller
{
    [Route("User/Error/403")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["Title"] = "دسترسی غیرمجاز";
        return View("403");
    }

    [Route("User/Error/404")]
    public IActionResult NotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        ViewData["Title"] = "صفحه یافت نشد";
        return View("404");
    }

    [Route("User/Error/500")]
    public IActionResult ServerError()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        ViewData["Title"] = "بروز خطای داخلی";
        return View("500");
    }
}
