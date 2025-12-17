using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Domain.Entities.Pages;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.ViewComponents;

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

