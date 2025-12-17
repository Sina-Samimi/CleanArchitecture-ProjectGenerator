using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TestAttarClone.Application.Commands.Catalog;
using TestAttarClone.Application.Queries.Catalog;
using TestAttarClone.Application.Queries.Sellers;
using TestAttarClone.SharedKernel.Authorization;
using TestAttarClone.SharedKernel.Extensions;
using TestAttarClone.WebSite.Areas.Seller.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TestAttarClone.WebSite.Areas.Seller.Controllers;

[Area("Seller")]
[Authorize(Policy = AuthorizationPolicies.SellerPanelAccess)]
public sealed class ProductCommentsController : Controller
{
    private readonly IMediator _mediator;

    public ProductCommentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> AllComments()
    {
        ViewData["Title"] = "تمام نظرات";
        ViewData["Subtitle"] = "مشاهده و مدیریت نظرات محصولات شما";
        ViewData["Sidebar:ActiveTab"] = "products";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var cancellationToken = HttpContext.RequestAborted;

        // Get all seller products
        var productsResult = await _mediator.Send(new GetSellerProductsQuery(userId), cancellationToken);
        if (!productsResult.IsSuccess || productsResult.Value is null || !productsResult.Value.Any())
        {
            TempData["Alert.Message"] = "محصول یافت نشد.";
            TempData["Alert.Type"] = "info";
            return RedirectToAction("Index", "Products");
        }

        var productDict = productsResult.Value.ToDictionary(p => p.Id, p => p.Name);
        var productIds = productDict.Keys.ToList();

        // Get all comments for seller's products
        var allComments = new List<(Guid ProductId, string ProductName, Application.DTOs.Catalog.ProductCommentDto CommentDto)>();
        
        foreach (var productId in productIds)
        {
            var commentsResult = await _mediator.Send(new GetProductCommentsQuery(productId), cancellationToken);
            if (commentsResult.IsSuccess && commentsResult.Value?.Comments is not null)
            {
                var productName = productDict[productId];
                foreach (var comment in commentsResult.Value.Comments)
                {
                    allComments.Add((productId, productName, comment));
                }
            }
        }

        // Separate new and approved comments
        var newComments = allComments
            .Where(c => !c.CommentDto.IsApproved)
            .OrderByDescending(c => c.CommentDto.CreatedAt)
            .Select(c => new SellerCommentItemViewModel(
                c.CommentDto.Id,
                c.ProductId,
                c.ProductName,
                c.CommentDto.AuthorName,
                c.CommentDto.Content,
                (int?)Math.Round(c.CommentDto.Rating),
                c.CommentDto.CreatedAt,
                c.CommentDto.IsApproved
            ))
            .ToList();

        var approvedComments = allComments
            .Where(c => c.CommentDto.IsApproved)
            .OrderByDescending(c => c.CommentDto.CreatedAt)
            .Select(c => new SellerCommentItemViewModel(
                c.CommentDto.Id,
                c.ProductId,
                c.ProductName,
                c.CommentDto.AuthorName,
                c.CommentDto.Content,
                (int?)Math.Round(c.CommentDto.Rating),
                c.CommentDto.CreatedAt,
                c.CommentDto.IsApproved
            ))
            .ToList();

        var viewModel = new SellerAllCommentsViewModel
        {
            NewComments = newComments,
            ApprovedComments = approvedComments
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid productId)
    {
        ViewData["Title"] = "نظرات محصول";
        ViewData["Subtitle"] = "مدیریت و پاسخ به نظرات مشتریان";
        ViewData["Sidebar:ActiveTab"] = "products";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var cancellationToken = HttpContext.RequestAborted;

        // Verify product ownership
        var productDetailResult = await _mediator.Send(new GetSellerProductDetailQuery(productId, userId), cancellationToken);
        if (!productDetailResult.IsSuccess || productDetailResult.Value is null)
        {
            TempData["Alert.Message"] = "محصول مورد نظر یافت نشد یا دسترسی به آن ندارید.";
            TempData["Alert.Type"] = "danger";
            return RedirectToAction("Index", "Products");
        }

        var product = productDetailResult.Value;

        // Get seller profile for default author name
        var sellerProfileResult = await _mediator.Send(new GetSellerProfileByUserIdQuery(userId), cancellationToken);
        var sellerDisplayName = sellerProfileResult.IsSuccess && sellerProfileResult.Value is not null
            ? sellerProfileResult.Value.DisplayName
            : "فروشنده";

        // Get comments
        var commentsResult = await _mediator.Send(new GetProductCommentsQuery(productId), cancellationToken);
        if (!commentsResult.IsSuccess || commentsResult.Value is null)
        {
            TempData["Alert.Message"] = commentsResult.Error ?? "دریافت نظرات با خطا مواجه شد.";
            TempData["Alert.Type"] = "danger";
            return RedirectToAction("Index", "Products");
        }

        var commentsData = commentsResult.Value;

        // Filter only approved comments for seller panel
        var approvedComments = commentsData.Comments
            .Where(c => c.IsApproved)
            .ToList();

        // Organize comments into threads (parent comments with their replies)
        var parentComments = approvedComments
            .Where(c => c.ParentId == null)
            .OrderBy(c => c.CreatedAt)
            .ToList();

        var replies = approvedComments
            .Where(c => c.ParentId != null)
            .GroupBy(c => c.ParentId)
            .ToDictionary(g => g.Key!.Value, g => g.OrderBy(c => c.CreatedAt).ToList());

        var commentViewModels = parentComments.Select(parent => new SellerProductCommentViewModel
        {
            Id = parent.Id,
            AuthorName = parent.AuthorName,
            Content = parent.Content,
            Rating = parent.Rating,
            IsApproved = parent.IsApproved,
            CreatedAt = parent.CreatedAt,
            UpdatedAt = parent.UpdatedAt,
            Replies = replies.ContainsKey(parent.Id)
                ? replies[parent.Id].Select(reply => new SellerProductCommentViewModel
                {
                    Id = reply.Id,
                    AuthorName = reply.AuthorName,
                    Content = reply.Content,
                    Rating = reply.Rating,
                    IsApproved = reply.IsApproved,
                    CreatedAt = reply.CreatedAt,
                    UpdatedAt = reply.UpdatedAt,
                    ParentId = reply.ParentId
                }).ToList()
                : new List<SellerProductCommentViewModel>()
        }).ToList();

        var viewModel = new SellerProductCommentsViewModel
        {
            ProductId = productId,
            ProductName = product.Name,
            DefaultAuthorName = sellerDisplayName,
            Comments = commentViewModels
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(Guid productId, Guid parentCommentId, SellerProductCommentReplyViewModel model)
    {
        ViewData["Title"] = "پاسخ به نظر";
        ViewData["Subtitle"] = "پاسخ به نظر مشتری";
        ViewData["Sidebar:ActiveTab"] = "products";

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["Alert.Message"] = "لطفاً تمام فیلدهای الزامی را پر کنید.";
            TempData["Alert.Type"] = "danger";
            return RedirectToAction(nameof(Index), new { productId });
        }

        var cancellationToken = HttpContext.RequestAborted;

        // Get seller profile for default author name if not provided
        var authorName = model.AuthorName;
        if (string.IsNullOrWhiteSpace(authorName))
        {
            var sellerProfileResult = await _mediator.Send(new GetSellerProfileByUserIdQuery(userId), cancellationToken);
            authorName = sellerProfileResult.IsSuccess && sellerProfileResult.Value is not null
                ? sellerProfileResult.Value.DisplayName
                : "فروشنده";
        }

        var command = new ReplyToProductCommentCommand(
            productId,
            parentCommentId,
            authorName.Trim(),
            model.Content.Trim(),
            userId);

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["Alert.Message"] = result.Error ?? "ارسال پاسخ با خطا مواجه شد.";
            TempData["Alert.Type"] = "danger";
        }
        else
        {
            TempData["Alert.Message"] = "پاسخ شما با موفقیت ثبت شد.";
            TempData["Alert.Type"] = "success";
        }

        return RedirectToAction(nameof(Index), new { productId });
    }
}

