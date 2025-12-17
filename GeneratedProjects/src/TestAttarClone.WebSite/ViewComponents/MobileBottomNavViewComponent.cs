using Microsoft.AspNetCore.Mvc;

namespace TestAttarClone.WebSite.ViewComponents;

public sealed class MobileBottomNavViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}

