using System.Threading.Tasks;
using Attar.Application.Queries.Pages;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.Controllers;

public sealed class PageController : Controller
{
    private readonly IMediator _mediator;

    public PageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("Page/{slug}")]
    public async Task<IActionResult> Index(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var cancellationToken = HttpContext.RequestAborted;

        var query = new GetPageBySlugQuery(slug);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return NotFound();
        }

        var page = result.Value;

        // Only show published pages to public
        if (!page.IsPublished)
        {
            return NotFound();
        }

        ViewData["Title"] = page.MetaTitle ?? page.Title;
        ViewData["MetaDescription"] = page.MetaDescription;
        ViewData["MetaKeywords"] = page.MetaKeywords;
        ViewData["MetaRobots"] = page.MetaRobots;

        return View(page);
    }
}

