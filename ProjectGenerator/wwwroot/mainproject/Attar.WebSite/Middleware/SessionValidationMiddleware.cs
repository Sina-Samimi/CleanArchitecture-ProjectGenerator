using System;
using System.Linq;
using System.Threading.Tasks;
using Attar.Application.Interfaces;
using Attar.WebSite.Services.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Attar.WebSite.Middleware;

public sealed class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionValidationMiddleware> _logger;

    public SessionValidationMiddleware(RequestDelegate next, ILogger<SessionValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISessionCookieService sessionCookieService,
        IUserSessionRepository userSessionRepository,
        UserManager<Domain.Entities.ApplicationUser> userManager,
        SignInManager<Domain.Entities.ApplicationUser> signInManager)
    {
        // Only check for authenticated users
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var currentSessionId = sessionCookieService.GetCurrentSessionId();
            if (currentSessionId.HasValue)
            {
                var session = await userSessionRepository.GetByIdAsync(currentSessionId.Value, context.RequestAborted);
                
                // If session is closed, logout the user
                if (session is null || session.SignedOutAt.HasValue)
                {
                    _logger.LogInformation("Session {SessionId} is closed, logging out user {UserId}", currentSessionId.Value, context.User.Identity.Name);
                    
                    // Clear session cookie
                    sessionCookieService.ClearCurrentSessionId();
                    
                    // Sign out the user
                    await signInManager.SignOutAsync();
                    
                    // Redirect to login page
                    context.Response.Redirect("/Account/PhoneLogin?sessionExpired=true");
                    return;
                }
                
                // Update LastSeenAt for active sessions
                if (session.SignedOutAt == null)
                {
                    session.Touch();
                    await userSessionRepository.UpdateAsync(session, context.RequestAborted);
                }
            }
        }

        await _next(context);
    }
}

