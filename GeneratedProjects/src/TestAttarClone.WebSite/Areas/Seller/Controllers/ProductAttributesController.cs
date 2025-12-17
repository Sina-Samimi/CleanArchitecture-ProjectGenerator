using System;
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
public sealed class ProductAttributesController : Controller
{
    private readonly IMediator _mediator;

    public ProductAttributesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid productId, Guid? attributeId)
    {
        ViewData["Sidebar:ActiveTab"] = "products";
        ViewData["Title"] = "مدیریت ویژگی‌های محصول";
        ViewData["Subtitle"] = "ویژگی‌های محصول را مدیریت کنید";

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var viewModelResult = await BuildProductAttributesViewModelAsync(productId, userId, cancellationToken, null, attributeId);

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
    public async Task<IActionResult> Create(Guid productId, SellerProductAttributeFormModel form)
    {
        ViewData["Sidebar:ActiveTab"] = "products";
        ViewData["Title"] = "افزودن ویژگی";
        ViewData["Subtitle"] = "ویژگی جدید برای محصول";

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var viewModelResult = await BuildProductAttributesViewModelAsync(productId, userId, cancellationToken, form);
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

        var command = new CreateProductAttributeCommand(productId, form.Key, form.Value, form.DisplayOrder);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "ثبت ویژگی انجام نشد.");
            var viewModelResult = await BuildProductAttributesViewModelAsync(productId, userId, cancellationToken, form);
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

        TempData["Success"] = "ویژگی با موفقیت ثبت شد.";
        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid productId, SellerProductAttributeFormModel form)
    {
        ViewData["Sidebar:ActiveTab"] = "products";
        ViewData["Title"] = "ویرایش ویژگی";
        ViewData["Subtitle"] = "ویرایش ویژگی محصول";

        var cancellationToken = HttpContext.RequestAborted;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (form.Id is null || form.Id == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "شناسه ویژگی معتبر نیست.");
        }

        if (!ModelState.IsValid)
        {
            var viewModelResult = await BuildProductAttributesViewModelAsync(productId, userId, cancellationToken, form, form.Id);
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

        var command = new UpdateProductAttributeCommand(
            productId,
            form.Id!.Value,
            form.Key,
            form.Value,
            form.DisplayOrder);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "بروزرسانی ویژگی انجام نشد.");
            var viewModelResult = await BuildProductAttributesViewModelAsync(productId, userId, cancellationToken, form, form.Id);
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

        TempData["Success"] = "ویژگی با موفقیت بروزرسانی شد.";
        return RedirectToAction(nameof(Index), new { productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid productId, Guid attributeId)
    {
        if (attributeId == Guid.Empty)
        {
            TempData["Error"] = "شناسه ویژگی معتبر نیست.";
            return RedirectToAction(nameof(Index), new { productId });
        }

        var cancellationToken = HttpContext.RequestAborted;
        var command = new DeleteProductAttributeCommand(productId, attributeId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "حذف ویژگی انجام نشد.";
        }
        else
        {
            TempData["Success"] = "ویژگی با موفقیت حذف شد.";
        }

        return RedirectToAction(nameof(Index), new { productId });
    }

    private async Task<Result<SellerProductAttributesViewModel>> BuildProductAttributesViewModelAsync(
        Guid productId,
        string userId,
        CancellationToken cancellationToken,
        SellerProductAttributeFormModel? form = null,
        Guid? editingAttributeId = null)
    {
        // Verify product ownership
        var productQuery = new GetSellerProductDetailQuery(productId, userId);
        var productResult = await _mediator.Send(productQuery, cancellationToken);
        if (!productResult.IsSuccess || productResult.Value is null)
        {
            return Result<SellerProductAttributesViewModel>.Failure("محصول مورد نظر یافت نشد یا شما اجازه دسترسی به آن را ندارید.");
        }

        var attributesQuery = new GetProductAttributesQuery(productId);
        var attributesResult = await _mediator.Send(attributesQuery, cancellationToken);
        if (!attributesResult.IsSuccess || attributesResult.Value is null)
        {
            return Result<SellerProductAttributesViewModel>.Failure(attributesResult.Error ?? "امکان دریافت اطلاعات ویژگی‌ها وجود ندارد.");
        }

        var viewModel = MapAttributes(attributesResult.Value, productResult.Value.Name, form, editingAttributeId);
        return Result<SellerProductAttributesViewModel>.Success(viewModel);
    }

    private static SellerProductAttributesViewModel MapAttributes(
        Application.DTOs.Catalog.ProductAttributesDto dto,
        string productName,
        SellerProductAttributeFormModel? form,
        Guid? editingAttributeId)
    {
        var attributes = dto.Items
            .OrderBy(attr => attr.DisplayOrder)
            .ThenBy(attr => attr.Key, StringComparer.CurrentCulture)
            .Select(attr => new SellerProductAttributeListItemViewModel(
                attr.Id,
                attr.Key,
                attr.Value,
                attr.DisplayOrder))
            .ToArray();

        var defaultOrder = attributes.Length == 0 ? 0 : attributes.Max(attr => attr.DisplayOrder) + 1;

        SellerProductAttributeFormModel formModel;
        if (form is not null)
        {
            formModel = new SellerProductAttributeFormModel
            {
                Id = form.Id,
                Key = form.Key,
                Value = form.Value,
                DisplayOrder = form.DisplayOrder
            };
        }
        else if (editingAttributeId.HasValue)
        {
            var editing = attributes.FirstOrDefault(attr => attr.Id == editingAttributeId.Value);
            formModel = editing is null
                ? new SellerProductAttributeFormModel { DisplayOrder = defaultOrder }
                : new SellerProductAttributeFormModel
                {
                    Id = editing.Id,
                    Key = editing.Key,
                    Value = editing.Value,
                    DisplayOrder = editing.DisplayOrder
                };
        }
        else
        {
            formModel = new SellerProductAttributeFormModel { DisplayOrder = defaultOrder };
        }

        if (formModel.DisplayOrder < 0)
        {
            formModel.DisplayOrder = 0;
        }

        var highlightId = editingAttributeId ?? form?.Id;

        return new SellerProductAttributesViewModel
        {
            ProductId = dto.ProductId,
            ProductName = productName,
            Attributes = attributes,
            Form = formModel,
            HighlightedAttributeId = highlightId
        };
    }
}

