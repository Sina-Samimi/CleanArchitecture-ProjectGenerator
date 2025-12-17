using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LogTableRenameTest.Application.DTOs.Cart;
using LogTableRenameTest.Application.Queries.Cart;
using LogTableRenameTest.WebSite.Models.Cart;
using LogTableRenameTest.WebSite.Services.Cart;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogTableRenameTest.WebSite.ViewComponents;

public sealed class CartPreviewViewComponent : ViewComponent
{
    private readonly IMediator _mediator;
    private readonly ICartCookieService _cartCookieService;

    public CartPreviewViewComponent(IMediator mediator, ICartCookieService cartCookieService)
    {
        _mediator = mediator;
        _cartCookieService = cartCookieService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string placement = CartPreviewViewModel.NavbarPlacement)
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var userId = GetUserId();
        var anonymousId = _cartCookieService.GetAnonymousCartId();

        var result = await _mediator.Send(new GetCartQuery(userId, anonymousId), cancellationToken);

        CartDto dto;
        if (!result.IsSuccess || result.Value is null)
        {
            dto = CartDtoMapper.CreateEmpty(anonymousId, userId);
        }
        else
        {
            dto = result.Value;
            UpdateCartCookie(dto.AnonymousId);
        }

        var model = CartViewModelFactory.CreatePreview(dto, placement);
        return View(model);
    }

    private string? GetUserId()
    {
        var principal = HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            return principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        return null;
    }

    private void UpdateCartCookie(Guid? anonymousId)
    {
        if (anonymousId is Guid id && id != Guid.Empty)
        {
            _cartCookieService.SetAnonymousCartId(id);
        }
    }
}
