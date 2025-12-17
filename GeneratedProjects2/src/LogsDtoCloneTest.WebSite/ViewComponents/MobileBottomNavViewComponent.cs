using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.ViewComponents;

public sealed class MobileBottomNavViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}

