using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Arsis.Application.Interfaces;
using Arsis.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EndPoint.WebSite.Authorization;

public sealed class TeacherPanelRequirement : IAuthorizationRequirement
{
}

public sealed class TeacherPanelAuthorizationHandler : AuthorizationHandler<TeacherPanelRequirement>
{
    private readonly ITeacherProfileRepository _teacherRepository;

    public TeacherPanelAuthorizationHandler(ITeacherProfileRepository teacherRepository)
    {
        _teacherRepository = teacherRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TeacherPanelRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (context.User.IsInRole(RoleNames.Teacher) || context.User.IsInRole(RoleNames.Admin))
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

        var teacherProfile = await _teacherRepository.GetByUserIdAsync(userId, cancellationToken);
        if (teacherProfile is not null && !teacherProfile.IsDeleted && teacherProfile.IsActive)
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
