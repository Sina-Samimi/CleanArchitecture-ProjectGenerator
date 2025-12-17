using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Commands.Catalog;
using TestAttarClone.Application.Queries.Catalog;
using TestAttarClone.SharedKernel.Authorization;
using TestAttarClone.SharedKernel.BaseTypes;
using TestAttarClone.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TestAttarClone.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class ProductVariantsController : Controller
{
    private readonly IMediator _mediator;

    public ProductVariantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid productId, Guid? variantId)
    {
        ViewData["Sidebar:ActiveTab"] = "products";
        ViewData["Title"] = "مدیریت گزینه‌های محصول";
        ViewData["Subtitle"] = "گزینه‌های محصول را مدیریت کنید";

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var viewModelResult = await BuildProductVariantsViewModelAsync(productId, userId, cancellationToken, null, variantId);

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
    public async Task<IActionResult> Create(Guid productId, SellerProductVariantFormModel form)
    {
        ViewData["Sidebar:ActiveTab"] = "products";
        ViewData["Title"] = "افزودن گزینه";
        ViewData["Subtitle"] = "گزینه جدید برای محصول";

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var viewModelResult = await BuildProductVariantsViewModelAsync(productId, userId, cancellationToken, form);
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

        // Build variant options from form
        var variantOptions = form.Options
            .Where(kv => kv.Key != Guid.Empty && !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => new CreateProductVariantCommand.VariantOption(kv.Key, kv.Value.Trim()))
            .ToList();

        if (variantOptions.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "لطفاً حداقل یک گزینه برای variant انتخاب کنید.");
            var viewModelResult = await BuildProductVariantsViewModelAsync(productId, userId, cancellationToken, form);
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

        var command = new CreateProductVariantCommand(
            productId,
            form.Price,
            form.CompareAtPrice,
            form.StockQuantity,
            form.Sku,
            form.ImagePath,
            form.IsActive,
            variantOptions);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "ثبت گزینه انجام نشد.");
            var viewModelResult = await BuildProductVariantsViewModelAsync(productId, userId, cancellationToken, form);
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

        TempData["Success"] = "گزینه با موفقیت ثبت شد.";
        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid productId, SellerProductVariantFormModel form)
    {
        ViewData["Sidebar:ActiveTab"] = "products";
        ViewData["Title"] = "ویرایش گزینه";
        ViewData["Subtitle"] = "ویرایش گزینه محصول";

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (form.Id is null || form.Id == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "شناسه گزینه معتبر نیست.");
        }

        if (!ModelState.IsValid)
        {
            var viewModelResult = await BuildProductVariantsViewModelAsync(productId, userId, cancellationToken, form, form.Id);
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

        // Build variant options from form
        var variantOptions = form.Options
            .Where(kv => kv.Key != Guid.Empty && !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => new UpdateProductVariantCommand.VariantOption(kv.Key, kv.Value.Trim()))
            .ToList();

        if (variantOptions.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "لطفاً حداقل یک گزینه برای variant انتخاب کنید.");
            var viewModelResult = await BuildProductVariantsViewModelAsync(productId, userId, cancellationToken, form, form.Id);
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

        var command = new UpdateProductVariantCommand(
            productId,
            form.Id!.Value,
            form.Price,
            form.CompareAtPrice,
            form.StockQuantity,
            form.Sku,
            form.ImagePath,
            form.IsActive,
            variantOptions);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "بروزرسانی گزینه انجام نشد.");
            var viewModelResult = await BuildProductVariantsViewModelAsync(productId, userId, cancellationToken, form, form.Id);
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

        TempData["Success"] = "گزینه با موفقیت بروزرسانی شد.";
        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid productId, Guid variantId)
    {
        if (variantId == Guid.Empty)
        {
            TempData["Error"] = "شناسه گزینه معتبر نیست.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = new DeleteProductVariantCommand(productId, variantId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "حذف گزینه انجام نشد.";
        }
        else
        {
            TempData["Success"] = "گزینه با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(Index), new { productId });
    }

    private async Task<Result<SellerProductVariantsViewModel>> BuildProductVariantsViewModelAsync(
        Guid productId,
        string userId,
        CancellationToken cancellationToken,
        SellerProductVariantFormModel? form = null,
        Guid? editingVariantId = null)
    {
        // Verify product ownership
        var productQuery = new GetSellerProductDetailQuery(productId, userId);
        var productResult = await _mediator.Send(productQuery, cancellationToken);
        if (!productResult.IsSuccess || productResult.Value is null)
        {
            return Result<SellerProductVariantsViewModel>.Failure("محصول مورد نظر یافت نشد یا شما اجازه دسترسی به آن را ندارید.");
        }

        // Get product detail with variants
        var productDetailQuery = new GetProductDetailQuery(productId);
        var productDetailResult = await _mediator.Send(productDetailQuery, cancellationToken);
        if (!productDetailResult.IsSuccess || productDetailResult.Value is null)
        {
            return Result<SellerProductVariantsViewModel>.Failure("امکان دریافت اطلاعات گزینه‌ها وجود ندارد.");
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

        var variants = productDetailResult.Value.Variants
            .OrderBy(v => v.StockQuantity)
            .ThenBy(v => v.Price)
            .Select(v => new SellerProductVariantListItemViewModel(
                v.Id,
                v.Price,
                v.CompareAtPrice,
                v.StockQuantity,
                v.Sku,
                v.ImagePath,
                v.IsActive,
                v.Options
                    .Select(opt => (opt.VariantAttributeId, opt.Value))
                    .ToArray()))
            .ToArray();

        SellerProductVariantFormModel formModel;
        if (form is not null)
        {
            formModel = new SellerProductVariantFormModel
            {
                Id = form.Id,
                Price = form.Price,
                CompareAtPrice = form.CompareAtPrice,
                StockQuantity = form.StockQuantity,
                Sku = form.Sku,
                ImagePath = form.ImagePath,
                IsActive = form.IsActive,
                Options = form.Options
            };
        }
        else if (editingVariantId.HasValue)
        {
            var editing = variants.FirstOrDefault(v => v.Id == editingVariantId.Value);
            formModel = editing is null
                ? new SellerProductVariantFormModel { IsActive = true }
                : new SellerProductVariantFormModel
                {
                    Id = editing.Id,
                    Price = editing.Price,
                    CompareAtPrice = editing.CompareAtPrice,
                    StockQuantity = editing.StockQuantity,
                    Sku = editing.Sku,
                    ImagePath = editing.ImagePath,
                    IsActive = editing.IsActive,
                    Options = editing.Options.ToDictionary(opt => opt.VariantAttributeId, opt => opt.Value)
                };
        }
        else
        {
            formModel = new SellerProductVariantFormModel { IsActive = true };
        }

        var highlightId = editingVariantId ?? form?.Id;

        return Result<SellerProductVariantsViewModel>.Success(new SellerProductVariantsViewModel
        {
            ProductId = productId,
            ProductName = productResult.Value.Name,
            VariantAttributes = variantAttributes,
            Variants = variants,
            Form = formModel,
            HighlightedVariantId = highlightId
        });
    }
}
