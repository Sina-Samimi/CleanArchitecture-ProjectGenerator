using System;
using System.Threading;
using System.Threading.Tasks;
using Attar.Application.DTOs.Sms;
using Attar.Application.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Attar.WebSite.Services;

/// <summary>
/// پیاده‌سازی <see cref="ISmsSender"/> که ارسال پیامک را به Hangfire واگذار می‌کند.
/// این کلاس فقط job را در صف قرار می‌دهد و خودِ ارسال توسط <see cref="ISMSSenderService"/> (مثلاً KavenegarSmsProvider) انجام می‌شود.
/// </summary>
public sealed class HangfireSmsSender : ISmsSender
{
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<HangfireSmsSender> _logger;

    public HangfireSmsSender(
        IBackgroundJobClient backgroundJobs,
        ILogger<HangfireSmsSender> logger)
    {
        _backgroundJobs = backgroundJobs ?? throw new ArgumentNullException(nameof(backgroundJobs));
        _logger = logger;
    }

    public Task SendVerificationCodeAsync(string phoneNumber, string code, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Verification code is required.", nameof(code));
        }

        var dto = new VerifyPhonenumberSmsDto(phoneNumber, code);

        // ارسال پیامک تأیید شماره تلفن را به Hangfire می‌سپاریم
        _backgroundJobs.Enqueue<ISMSSenderService>(svc => svc.PhoneNumberConfirmSms(dto));

        _logger.LogInformation("Enqueued PhoneNumberConfirmSms via Hangfire for phone {Phone}", phoneNumber);

        return Task.CompletedTask;
    }
}


