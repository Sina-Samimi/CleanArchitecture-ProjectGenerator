using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Entities.Pages;
using Microsoft.AspNetCore.Mvc;

namespace TestAttarClone.WebSite.ViewComponents;

public sealed class QuickAccessPagesViewComponent : ViewComponent
{
    private readonly IPageRepository _pageRepository;

    public QuickAccessPagesViewComponent(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var pages = await _pageRepository.GetQuickAccessPagesAsync(HttpContext.RequestAborted);
        return View(pages);
    }
}

