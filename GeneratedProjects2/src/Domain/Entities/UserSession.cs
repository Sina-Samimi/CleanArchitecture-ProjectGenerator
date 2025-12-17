using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using LogsDtoCloneTest.Domain.Base;

namespace LogsDtoCloneTest.Domain.Entities;

public sealed class UserSession : Entity
{
    [SetsRequiredMembers]
    private UserSession()
    {
    }

    [SetsRequiredMembers]
    private UserSession(
        string userId,
        IPAddress ipAddress,
        string deviceType,
        string clientName,
        string userAgent)
    {
        UserId = userId;
        DeviceType = deviceType;
        ClientName = clientName;
        UserAgent = userAgent;
        SignedInAt = DateTimeOffset.UtcNow;
        LastSeenAt = SignedInAt;
        CreatorId = userId;
        CreateDate = SignedInAt;
        UpdateDate = SignedInAt;
        Ip = ipAddress;
    }

    public string UserId { get; private set; } = string.Empty;

    public string DeviceType { get; private set; } = string.Empty;

    public string ClientName { get; private set; } = string.Empty;

    public string UserAgent { get; private set; } = string.Empty;

    public DateTimeOffset SignedInAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public DateTimeOffset? SignedOutAt { get; private set; }

    public ApplicationUser User { get; private set; } = null!;

    public static UserSession Start(
        string userId,
        IPAddress? ipAddress,
        string? deviceType,
        string? clientName,
        string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id cannot be null or whitespace.", nameof(userId));
        }

        var normalizedDeviceType = string.IsNullOrWhiteSpace(deviceType)
            ? "Unknown"
            : deviceType.Trim();
        var normalizedClientName = string.IsNullOrWhiteSpace(clientName)
            ? "Unknown"
            : clientName.Trim();
        var normalizedUserAgent = string.IsNullOrWhiteSpace(userAgent)
            ? "Unknown"
            : userAgent.Trim();

        return new UserSession(
            userId,
            ipAddress ?? IPAddress.None,
            normalizedDeviceType,
            normalizedClientName,
            normalizedUserAgent);
    }

    public void Touch()
    {
        var now = DateTimeOffset.UtcNow;
        LastSeenAt = now;
        UpdateDate = now;
    }

    public void Close()
    {
        if (SignedOutAt.HasValue)
        {
            return;
        }

        SignedOutAt = DateTimeOffset.UtcNow;
        UpdateDate = SignedOutAt.Value;
    }
}
