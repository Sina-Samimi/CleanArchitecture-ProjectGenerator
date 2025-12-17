using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Attar.WebSite.Services;

public sealed class LoggingSmsSender : ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationCodeAsync(string phoneNumber, string code, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        _logger.LogInformation("Sending verification code {Code} to {PhoneNumber}. Lifetime: {Lifetime} seconds.", code, phoneNumber, lifetime.TotalSeconds);
        return Task.CompletedTask;
    }
}
