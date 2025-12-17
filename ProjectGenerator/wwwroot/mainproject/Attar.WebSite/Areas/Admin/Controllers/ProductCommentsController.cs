using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Attar.Application.Queries.Catalog;
using Attar.WebSite.Areas.Admin.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Attar.WebSite.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class ProductCommentsController : Controller
{
    private readonly IMediator _mediator;

    public ProductCommentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "نظرات در انتظار بررسی";
        ViewData["Subtitle"] = "لیست نظرات محصولات در انتظار تایید یا رد";

        var result = await _mediator.Send(new GetPendingProductCommentsQuery(), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "دریافت نظرات در انتظار با خطا مواجه شد.";
            return View(new PendingProductCommentListViewModel { Items = System.Array.Empty<PendingProductCommentItemViewModel>(), TotalCount = 0 });
        }

        var dto = result.Value;

        var vm = new PendingProductCommentListViewModel
        {
            TotalCount = dto.TotalCount,
            Items = dto.Items.Select(i => new PendingProductCommentItemViewModel
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductSlug = i.ProductSlug,
                AuthorName = i.AuthorName,
                Excerpt = i.Excerpt,
                CreatedAt = i.CreatedAt
            }).ToArray()
        };

        return View(vm);
    }
}
