using System;
using System.Net;
using System.Security.Claims;
using TestAttarClone.Application.Interfaces;
using TestAttarClone.Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace TestAttarClone.Infrastructure.Services;

public sealed class HttpAuditContext : IAuditContext
{
    private static readonly IPAddress LoopbackAddress = IPAddress.Loopback;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpAuditContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuditMetadata Capture()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var userId = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            userId = SystemUsers.AutomationId;
        }

        var remoteAddress = httpContext?.Connection.RemoteIpAddress;
        if (remoteAddress is null || remoteAddress.Equals(IPAddress.None) || remoteAddress.Equals(IPAddress.IPv6None))
        {
            remoteAddress = LoopbackAddress;
        }

        return new AuditMetadata(userId, remoteAddress, DateTimeOffset.UtcNow);
    }
}
