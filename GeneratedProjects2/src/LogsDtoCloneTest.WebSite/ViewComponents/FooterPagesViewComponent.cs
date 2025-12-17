using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Entities.Pages;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.ViewComponents;

public sealed class FooterPagesViewComponent : ViewComponent
{
    private readonly IPageRepository _pageRepository;

    public FooterPagesViewComponent(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var pages = await _pageRepository.GetFooterPagesAsync(HttpContext.RequestAborted);
        return View(pages);
    }
}

