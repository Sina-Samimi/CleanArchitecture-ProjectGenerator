using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Attar.Application.Interfaces;
using Attar.Domain.Entities.Pages;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.ViewComponents;

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

