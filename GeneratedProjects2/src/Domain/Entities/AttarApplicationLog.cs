using System.Diagnostics.CodeAnalysis;
using System.Net;
using LogsDtoCloneTest.Domain.Base;

namespace LogsDtoCloneTest.Domain.Entities;

/// <summary>
/// Entity برای ذخیره لاگ‌های خطا و هشدار در دیتابیس
/// </summary>
public sealed class AttarApplicationLog : Entity
{
    public string Level { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public string? Exception { get; private set; }

    public string? SourceContext { get; private set; }

    public string? Properties { get; private set; }

    public string? RequestPath { get; private set; }

    public string? RequestMethod { get; private set; }

    public int? StatusCode { get; private set; }

    public double? ElapsedMs { get; private set; }

    public string? UserAgent { get; private set; }

    public string? RemoteIpAddress { get; private set; }

    public string? ApplicationName { get; private set; }

    public string? MachineName { get; private set; }

    public string? Environment { get; private set; }

    [SetsRequiredMembers]
    private AttarApplicationLog()
    {
    }

    [SetsRequiredMembers]
    public AttarApplicationLog(
        string level,
        string message,
        string? exception = null,
        string? sourceContext = null,
        string? properties = null,
        string? requestPath = null,
        string? requestMethod = null,
        int? statusCode = null,
        double? elapsedMs = null,
        string? userAgent = null,
        string? remoteIpAddress = null,
        string? applicationName = null,
        string? machineName = null,
        string? environment = null,
        IPAddress? ipAddress = null)
    {
        Level = level;
        Message = message;
        Exception = exception;
        SourceContext = sourceContext;
        Properties = properties;
        RequestPath = requestPath;
        RequestMethod = requestMethod;
        StatusCode = statusCode;
        ElapsedMs = elapsedMs;
        UserAgent = userAgent;
        RemoteIpAddress = remoteIpAddress;
        ApplicationName = applicationName;
        MachineName = machineName;
        Environment = environment;
        
        if (ipAddress is not null)
        {
            Ip = ipAddress;
        }
    }
}

