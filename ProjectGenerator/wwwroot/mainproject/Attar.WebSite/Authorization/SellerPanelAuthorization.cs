using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.Interfaces;
using Attar.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Attar.WebSite.Authorization;

public sealed class SellerPanelRequirement : IAuthorizationRequirement
{
}

public sealed class SellerPanelAuthorizationHandler : AuthorizationHandler<SellerPanelRequirement>
{
    private readonly ISellerProfileRepository _sellerRepository;

    public SellerPanelAuthorizationHandler(ISellerProfileRepository sellerRepository)
    {
        _sellerRepository = sellerRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SellerPanelRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (context.User.IsInRole(RoleNames.Seller) || context.User.IsInRole(RoleNames.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var httpContext = ResolveHttpContext(context.Resource);
        var cancellationToken = httpContext?.RequestAborted ?? CancellationToken.None;

        var sellerProfile = await _sellerRepository.GetByUserIdAsync(userId, cancellationToken);
        if (sellerProfile is not null && !sellerProfile.IsDeleted && sellerProfile.IsActive)
        {
            context.Succeed(requirement);
        }
    }

    private static HttpContext? ResolveHttpContext(object? resource)
    {
        return resource switch
        {
            HttpContext httpContext => httpContext,
            AuthorizationFilterContext filterContext => filterContext.HttpContext,
            _ => null
        };
    }
}
