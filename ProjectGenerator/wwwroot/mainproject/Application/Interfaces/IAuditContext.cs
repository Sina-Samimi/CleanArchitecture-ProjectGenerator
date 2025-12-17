using System;
using System.Net;

namespace Attar.Application.Interfaces;

public interface IAuditContext
{
    AuditMetadata Capture();
}

public readonly record struct AuditMetadata(string UserId, IPAddress IpAddress, DateTimeOffset Timestamp);
