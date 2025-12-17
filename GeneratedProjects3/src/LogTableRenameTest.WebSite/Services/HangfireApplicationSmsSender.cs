using System;
using System.Threading.Tasks;
using LogTableRenameTest.Application.DTOs.Sms;
using LogTableRenameTest.Application.Interfaces;
using LogTableRenameTest.Infrastructure.Services;
using LogTableRenameTest.SharedKernel.DTOs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace LogTableRenameTest.WebSite.Services;

/// <summary>
/// پیاده‌سازی <see cref="ISMSSenderService"/> که تمام ارسال‌های پیامک را
/// به Hangfire واگذار می‌کند تا سیستم اصلی درگیر تاخیرهای شبکه Kavenegar نشود.
/// </summary>
public sealed class HangfireApplicationSmsSender : ISMSSenderService
{
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<HangfireApplicationSmsSender> _logger;

    public HangfireApplicationSmsSender(
        IBackgroundJobClient backgroundJobs,
        ILogger<HangfireApplicationSmsSender> logger)
    {
        _backgroundJobs = backgroundJobs ?? throw new ArgumentNullException(nameof(backgroundJobs));
        _logger = logger;
    }

    private static ResponseDto CreateEnqueuedResponse()
    {
        var response = new ResponseDto
        {
            Success = true,
            Code = 202
        };
        response.Messages.Add(new Messages
        {
            message = "پیامک در صف ارسال قرار گرفت."
        });
        return response;
    }

    public Task<ResponseDto> PhoneNumberConfirmSms(VerifyPhonenumberSmsDto verifyPhonenumber)
    {
        if (verifyPhonenumber is null) throw new ArgumentNullException(nameof(verifyPhonenumber));

        _backgroundJobs.Enqueue<KavenegarSmsProvider>(svc => svc.PhoneNumberConfirmSms(verifyPhonenumber));
        _logger.LogInformation("Enqueued PhoneNumberConfirmSms via Hangfire for {Phone}", verifyPhonenumber.PhoneNumber);

        return Task.FromResult(CreateEnqueuedResponse());
    }

    public Task<ResponseDto> TicketReplySms(TicketReplySmsDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        _backgroundJobs.Enqueue<KavenegarSmsProvider>(svc => svc.TicketReplySms(dto));
        _logger.LogInformation("Enqueued TicketReplySms via Hangfire for {Phone}", dto.PhoneNumber);

        return Task.FromResult(CreateEnqueuedResponse());
    }

    public Task<ResponseDto> WithdrawalResponseSms(WithdrawalResponseSmsDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        _backgroundJobs.Enqueue<KavenegarSmsProvider>(svc => svc.WithdrawalResponseSms(dto));
        _logger.LogInformation("Enqueued WithdrawalResponseSms via Hangfire for {Phone}", dto.PhoneNumber);

        return Task.FromResult(CreateEnqueuedResponse());
    }

    public Task<ResponseDto> ProductFollowUpStatusSms(ProductFollowUpStatusSmsDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        _backgroundJobs.Enqueue<KavenegarSmsProvider>(svc => svc.ProductFollowUpStatusSms(dto));
        _logger.LogInformation("Enqueued ProductFollowUpStatusSms via Hangfire for {Phone}", dto.PhoneNumber);

        return Task.FromResult(CreateEnqueuedResponse());
    }

    public Task<ResponseDto> SellerProductRequestReplySms(SellerProductRequestReplySmsDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        _backgroundJobs.Enqueue<KavenegarSmsProvider>(svc => svc.SellerProductRequestReplySms(dto));
        _logger.LogInformation("Enqueued SellerProductRequestReplySms via Hangfire for {Phone}", dto.PhoneNumber);

        return Task.FromResult(CreateEnqueuedResponse());
    }

    public Task<ResponseDto> BackInStockSms(BackInStockSmsDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        _backgroundJobs.Enqueue<KavenegarSmsProvider>(svc => svc.BackInStockSms(dto));
        _logger.LogInformation("Enqueued BackInStockSms via Hangfire for {Phone}", dto.PhoneNumber);

        return Task.FromResult(CreateEnqueuedResponse());
    }
}


