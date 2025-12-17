using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.ViewComponents;

public sealed class MobileBottomNavViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}

