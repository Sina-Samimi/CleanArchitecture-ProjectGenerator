using System.Collections.Concurrent;
using MobiRooz.Domain.Entities;
using MobiRooz.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace MobiRooz.Infrastructure.Services.Logging;

/// <summary>
/// Custom Serilog Sink برای نوشتن لاگ‌های Error/Warning در دیتابیس
/// با Performance بهینه (Batch Writing)
/// </summary>
public sealed class ApplicationLogSink : ILogEventSink, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentQueue<LogEvent> _logQueue = new();
    private readonly Timer _timer;
    private readonly int _batchSize = 50; // تعداد لاگ برای Batch Writing
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(5); // هر 5 ثانیه یکبار Flush
    private readonly object _lock = new();
    private volatile bool _disposed;

    public ApplicationLogSink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _timer = new Timer(FlushLogs, null, _flushInterval, _flushInterval);
    }

    public void Emit(LogEvent logEvent)
    {
        if (_disposed)
            return;

        // فقط Error و Warning را در صف قرار بده
        if (logEvent.Level < LogEventLevel.Warning)
            return;

        _logQueue.Enqueue(logEvent);

        // اگر صف پر شد، فوراً Flush کن
        if (_logQueue.Count >= _batchSize)
        {
            Task.Run(() => FlushLogs(null));
        }
    }

    private void FlushLogs(object? state = null)
    {
        if (_disposed || _logQueue.IsEmpty)
            return;

        lock (_lock)
        {
            if (_logQueue.IsEmpty)
                return;

            var logsToSave = new List<LogEvent>();

            // تمام لاگ‌ها را از صف بردار
            while (_logQueue.TryDequeue(out var logEvent) && logsToSave.Count < _batchSize)
            {
                logsToSave.Add(logEvent);
            }

            if (logsToSave.Count == 0)
                return;

            // در یک Scope جدید برای DbContext کار کن (از دیتابیس جداگانه لاگ استفاده می‌کنیم)
            try
            {
                using var scope = _serviceProvider.CreateScope();
                using var logsDbContext = scope.ServiceProvider.GetRequiredService<LogsDbContext>();

                var applicationLogs = logsToSave
                    .Select(logEvent => CreateApplicationLog(logEvent))
                    .ToList();

                logsDbContext.ApplicationLogs.AddRange(applicationLogs);
                logsDbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                // اگر خطا در نوشتن لاگ پیش آمد، لاگ کن (بدون exception throw)
                try
                {
                    var logger = _serviceProvider.GetService<ILogger<ApplicationLogSink>>();
                    logger?.LogError(ex, "Error saving application logs to database");
                }
                catch
                {
                    // Ignore logging errors
                }
            }
        }
    }

    private static ApplicationLog CreateApplicationLog(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage();
        var exception = logEvent.Exception?.ToString();

        // Extract properties
        var properties = new Dictionary<string, string?>();
        foreach (var property in logEvent.Properties)
        {
            properties[property.Key] = property.Value.ToString();
        }

        var propertiesJson = System.Text.Json.JsonSerializer.Serialize(properties);

        // Extract common properties
        var sourceContext = properties.TryGetValue("SourceContext", out var source) ? source : null;
        var requestPath = properties.TryGetValue("RequestPath", out var path) ? path : null;
        var requestMethod = properties.TryGetValue("RequestMethod", out var method) ? method : null;
        int? statusCode = null;
        if (properties.TryGetValue("StatusCode", out var status) && int.TryParse(status, out var code))
        {
            statusCode = code;
        }
        
        double? elapsed = null;
        if (properties.TryGetValue("Elapsed", out var elapsedStr) && double.TryParse(elapsedStr?.Replace(" ms", ""), out var elapsedMs))
        {
            elapsed = elapsedMs;
        }
        var userAgent = properties.TryGetValue("UserAgent", out var ua) ? ua : null;
        var remoteIp = properties.TryGetValue("RemoteIpAddress", out var ip) ? ip : null;
        var applicationName = properties.TryGetValue("ApplicationName", out var appName) ? appName : null;
        var machineName = properties.TryGetValue("MachineName", out var machine) ? machine : null;
        var environment = properties.TryGetValue("Environment", out var env) ? env : null;

        // Extract IP Address if available
        System.Net.IPAddress? ipAddress = null;
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            System.Net.IPAddress.TryParse(remoteIp, out ipAddress);
        }

        return new ApplicationLog(
            level: logEvent.Level.ToString(),
            message: message,
            exception: exception,
            sourceContext: sourceContext,
            properties: propertiesJson,
            requestPath: requestPath,
            requestMethod: requestMethod,
            statusCode: statusCode,
            elapsedMs: elapsed,
            userAgent: userAgent,
            remoteIpAddress: remoteIp,
            applicationName: applicationName,
            machineName: machineName,
            environment: environment,
            ipAddress: ipAddress ?? System.Net.IPAddress.None);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _timer?.Dispose();

        // آخرین بار لاگ‌ها را Flush کن
        FlushLogs();
    }
}

