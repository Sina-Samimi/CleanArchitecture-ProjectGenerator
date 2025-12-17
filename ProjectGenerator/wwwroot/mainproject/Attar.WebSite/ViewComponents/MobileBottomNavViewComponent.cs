using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.ViewComponents;

public sealed class MobileBottomNavViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}

