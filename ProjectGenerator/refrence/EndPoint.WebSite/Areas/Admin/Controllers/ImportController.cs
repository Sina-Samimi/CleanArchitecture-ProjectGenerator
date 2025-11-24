using EndPoint.WebSite.Growth;
using Microsoft.AspNetCore.Mvc;

namespace EndPoint.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class ImportController : Controller
{
    private readonly IQuestionImporter _importer;
    private readonly ILogger<ImportController> _logger;

    public ImportController(IQuestionImporter importer, ILogger<ImportController> logger)
    {
        _importer = importer;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedDefaults(CancellationToken cancellationToken)
    {
        try
        {
            await _importer.ImportCliftonAsync(string.Empty, cancellationToken);
            await _importer.ImportPvqAsync(string.Empty, cancellationToken);
            TempData["Success"] = "سؤالات پیش‌فرض کلیفتون و PVQ بارگذاری شدند.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در بارگذاری سؤالات پیش‌فرض");
            TempData["Error"] = "در هنگام بارگذاری سؤالات پیش‌فرض خطایی رخ داد.";
        }

        return RedirectToAction(nameof(Index));
    }
}
