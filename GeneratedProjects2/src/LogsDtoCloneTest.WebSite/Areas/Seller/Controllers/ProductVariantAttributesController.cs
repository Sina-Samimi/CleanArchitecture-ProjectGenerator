using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Commands.Catalog;
using LogsDtoCloneTest.Application.Queries.Catalog;
using LogsDtoCloneTest.SharedKernel.Authorization;
using LogsDtoCloneTest.SharedKernel.BaseTypes;
using LogsDtoCloneTest.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class ProductVariantAttributesController : Controller
{
    private readonly IMediator _mediator;

    public ProductVariantAttributesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid productId, Guid? variantAttributeId)
    {
        ViewData["Sidebar:ActiveTab"] = "products";
        ViewData["Title"] = "مدیریت ویژگی‌های گزینه‌ها";
        ViewData["Subtitle"] = "ویژگی‌های گزینه‌های محصول را مدیریت کنید";

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var viewModelResult = await BuildProductVariantAttributesViewModelAsync(productId, userId, cancellationToken, null, variantAttributeId);

        if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
        {
            if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
            {
                TempData["Error"] = viewModelResult.Error;
            }

            return RedirectToAction("Index", "Products");
        }

        return View(viewModelResult.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid productId, SellerProductVariantAttributeFormModel form)
    {
        ViewData["Sidebar:ActiveTab"] = "products";
        ViewData["Title"] = "افزودن ویژگی گزینه";
        ViewData["Subtitle"] = "ویژگی جدید برای گزینه‌های محصول";

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var viewModelResult = await BuildProductVariantAttributesViewModelAsync(productId, userId, cancellationToken, form);
            if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
            {
                if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
                {
                    TempData["Error"] = viewModelResult.Error;
                }

                return RedirectToAction("Index", "Products");
            }

            return View("Index", viewModelResult.Value);
        }

        var options = string.IsNullOrWhiteSpace(form.OptionsText)
            ? new List<string>()
            : form.OptionsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .ToList();

        var command = new CreateProductVariantAttributeCommand(productId, form.Name.Trim(), options, form.DisplayOrder);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "ثبت ویژگی گزینه انجام نشد.");
            var viewModelResult = await BuildProductVariantAttributesViewModelAsync(productId, userId, cancellationToken, form);
            if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
            {
                if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
                {
                    TempData["Error"] = viewModelResult.Error;
                }

                return RedirectToAction("Index", "Products");
            }

            return View("Index", viewModelResult.Value);
        }

        TempData["Success"] = "ویژگی گزینه با موفقیت ثبت شد.";
        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid productId, SellerProductVariantAttributeFormModel form)
    {
        ViewData["Sidebar:ActiveTab"] = "products";
        ViewData["Title"] = "ویرایش ویژگی گزینه";
        ViewData["Subtitle"] = "ویرایش ویژگی گزینه محصول";

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (form.Id is null || form.Id == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "شناسه ویژگی گزینه معتبر نیست.");
        }

        if (!ModelState.IsValid)
        {
            var viewModelResult = await BuildProductVariantAttributesViewModelAsync(productId, userId, cancellationToken, form, form.Id);
            if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
            {
                if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
                {
                    TempData["Error"] = viewModelResult.Error;
                }

                return RedirectToAction("Index", "Products");
            }

            return View("Index", viewModelResult.Value);
        }

        var options = string.IsNullOrWhiteSpace(form.OptionsText)
            ? new List<string>()
            : form.OptionsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .ToList();

        var command = new UpdateProductVariantAttributeCommand(
            productId,
            form.Id!.Value,
            form.Name.Trim(),
            options,
            form.DisplayOrder);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "بروزرسانی ویژگی گزینه انجام نشد.");
            var viewModelResult = await BuildProductVariantAttributesViewModelAsync(productId, userId, cancellationToken, form, form.Id);
            if (!viewModelResult.IsSuccess || viewModelResult.Value is null)
            {
                if (!string.IsNullOrWhiteSpace(viewModelResult.Error))
                {
                    TempData["Error"] = viewModelResult.Error;
                }

                return RedirectToAction("Index", "Products");
            }

            return View("Index", viewModelResult.Value);
        }

        TempData["Success"] = "ویژگی گزینه با موفقیت بروزرسانی شد.";
        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid productId, Guid variantAttributeId)
    {
        if (variantAttributeId == Guid.Empty)
        {
            TempData["Error"] = "شناسه ویژگی گزینه معتبر نیست.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = new DeleteProductVariantAttributeCommand(productId, variantAttributeId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "حذف ویژگی گزینه انجام نشد.";
        }
        else
        {
            TempData["Success"] = "ویژگی گزینه با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(Index), new { productId });
    }

    private async Task<Result<SellerProductVariantAttributesViewModel>> BuildProductVariantAttributesViewModelAsync(
        Guid productId,
        string userId,
        CancellationToken cancellationToken,
        SellerProductVariantAttributeFormModel? form = null,
        Guid? editingVariantAttributeId = null)
    {
        // Verify product ownership
        var productQuery = new GetSellerProductDetailQuery(productId, userId);
        var productResult = await _mediator.Send(productQuery, cancellationToken);
        if (!productResult.IsSuccess || productResult.Value is null)
        {
            return Result<SellerProductVariantAttributesViewModel>.Failure("محصول مورد نظر یافت نشد یا شما اجازه دسترسی به آن را ندارید.");
        }

        // Get product detail with variants
        var productDetailQuery = new GetProductDetailQuery(productId);
        var productDetailResult = await _mediator.Send(productDetailQuery, cancellationToken);
        if (!productDetailResult.IsSuccess || productDetailResult.Value is null)
        {
            return Result<SellerProductVariantAttributesViewModel>.Failure("امکان دریافت اطلاعات ویژگی‌های گزینه‌ها وجود ندارد.");
        }

        var variantAttributes = productDetailResult.Value.VariantAttributes
            .OrderBy(attr => attr.DisplayOrder)
            .ThenBy(attr => attr.Name)
            .Select(attr => new SellerProductVariantAttributeListItemViewModel(
                attr.Id,
                attr.Name,
                attr.Options,
                attr.DisplayOrder))
            .ToArray();

        var defaultOrder = variantAttributes.Length == 0 ? 0 : variantAttributes.Max(attr => attr.DisplayOrder) + 1;

        SellerProductVariantAttributeFormModel formModel;
        if (form is not null)
        {
            formModel = new SellerProductVariantAttributeFormModel
            {
                Id = form.Id,
                Name = form.Name,
                OptionsText = form.OptionsText,
                Options = form.Options,
                DisplayOrder = form.DisplayOrder
            };
        }
        else if (editingVariantAttributeId.HasValue)
        {
            var editing = variantAttributes.FirstOrDefault(attr => attr.Id == editingVariantAttributeId.Value);
            formModel = editing is null
                ? new SellerProductVariantAttributeFormModel { DisplayOrder = defaultOrder }
                : new SellerProductVariantAttributeFormModel
                {
                    Id = editing.Id,
                    Name = editing.Name,
                    OptionsText = string.Join(", ", editing.Options),
                    Options = editing.Options.ToList(),
                    DisplayOrder = editing.DisplayOrder
                };
        }
        else
        {
            formModel = new SellerProductVariantAttributeFormModel { DisplayOrder = defaultOrder };
        }

        if (formModel.DisplayOrder < 0)
        {
            formModel.DisplayOrder = 0;
        }

        var highlightId = editingVariantAttributeId ?? form?.Id;

        return Result<SellerProductVariantAttributesViewModel>.Success(new SellerProductVariantAttributesViewModel
        {
            ProductId = productId,
            ProductName = productResult.Value.Name,
            VariantAttributes = variantAttributes,
            Form = formModel,
            HighlightedVariantAttributeId = highlightId
        });
    }
}
