using LogsDtoCloneTest.Application.DTOs.Sms;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.SharedKernel.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LogsDtoCloneTest.SharedKernel.Helpers;

namespace LogsDtoCloneTest.Infrastructure.Services;

public sealed class KavenegarSmsProvider : ISMSSenderService
{
    private readonly ISmsSettingRepository _smsSettingRepository;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<KavenegarSmsProvider> _logger;

    public KavenegarSmsProvider(
        ISmsSettingRepository smsSettingRepository,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<KavenegarSmsProvider> logger)
    {
        _smsSettingRepository = smsSettingRepository;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<ResponseDto> PhoneNumberConfirmSms(VerifyPhonenumberSmsDto verifyPhonenumber)
    {
        var response = new ResponseDto();
        try
        {
            var result = await SendLookupAsync(
                phoneNumber: verifyPhonenumber.PhoneNumber,
                template: "PhoneNumberConfirmSms",
                token: verifyPhonenumber.Code,
                token2: null,
                token3: null,
                CancellationToken.None);
            return result;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Code = 500;
            response.Messages.Add(new Messages { message = ex.Message });
            return await Task.FromResult(response);
        }
    }

    public Task<ResponseDto> TicketReplySms(TicketReplySmsDto dto) =>
        SendLookupAsync(
            phoneNumber: dto.PhoneNumber,
            template: "TicketReplySms",
            token: dto.FirstName,
            token2: dto.LastName,
            token3: dto.TicketTitle,
            cancellationToken: CancellationToken.None);

    public Task<ResponseDto> WithdrawalResponseSms(WithdrawalResponseSmsDto dto) =>
        SendLookupAsync(
            phoneNumber: dto.PhoneNumber,
            template: "WithdrawalResponseSms",
            token: dto.FirstName,
            token2: dto.LastName,
            token3: dto.StatusText,
            cancellationToken: CancellationToken.None);

    public Task<ResponseDto> ProductFollowUpStatusSms(ProductFollowUpStatusSmsDto dto) =>
        SendLookupAsync(
            phoneNumber: dto.PhoneNumber,
            template: "ProductFollowUpStatusSms",
            token: dto.FirstName,
            token2: dto.ProductName,
            token3: dto.StatusText,
            cancellationToken: CancellationToken.None);

    public Task<ResponseDto> SellerProductRequestReplySms(SellerProductRequestReplySmsDto dto) =>
        SendLookupAsync(
            phoneNumber: dto.PhoneNumber,
            template: "SellerProductRequestReplySms",
            token: dto.FirstName,
            token2: dto.ProductName,
            token3: dto.StatusText,
            cancellationToken: CancellationToken.None);

    public Task<ResponseDto> BackInStockSms(BackInStockSmsDto dto) =>
        SendLookupAsync(
            phoneNumber: dto.PhoneNumber,
            template: "BackInStockSms",
            token: dto.ProductName,
            token2: dto.SellerName,
            token3: null,
            cancellationToken: CancellationToken.None);

    private async Task<ResponseDto> SendLookupAsync(
        string phoneNumber,
        string template,
        string token,
        string? token2,
        string? token3,
        CancellationToken cancellationToken)
    {
        var response = new ResponseDto();

        // Skip real sending in development
        if (_environment.IsDevelopment())
        {
            response.Success = true;
            response.Code = 200;
            response.Messages.Add(new Messages { message = $"SMS skipped in development. Template: {template}" });
            _logger.LogInformation("SMS skipped (dev): Template={Template}, Phone={Phone}, Tokens={Token}/{Token2}/{Token3}", template, phoneNumber, token, token2, token3);
            return await Task.FromResult(response);
        }

        try
        {
            var smsSetting = await _smsSettingRepository.GetCurrentAsync(cancellationToken);

            if (smsSetting is null || !smsSetting.IsActive)
            {
                response.Success = false;
                response.Code = 503;
                response.Messages.Add(new Messages { message = "سرویس پیامک فعال نیست. لطفاً تنظیمات را بررسی کنید." });
                return await Task.FromResult(response);
            }

            // Validate and normalize phone number before sending
            if (!PhoneNumberHelper.IsValid(phoneNumber))
            {
                response.Success = false;
                response.Code = 400;
                response.Messages.Add(new Messages { message = "شماره تلفن نامعتبر است." });
                _logger.LogWarning("Invalid phone number skipped for SMS send: {Phone}", phoneNumber);
                return await Task.FromResult(response);
            }

            var digits = PhoneNumberHelper.ExtractDigits(phoneNumber) ?? string.Empty;
            string standardized;
            if (digits.StartsWith("0", StringComparison.Ordinal))
            {
                standardized = "98" + digits[1..];
            }
            else if (digits.StartsWith("0098", StringComparison.Ordinal))
            {
                standardized = digits[2..];
            }
            else
            {
                standardized = digits;
            }

            var api = new Kavenegar.KavenegarApi(smsSetting.ApiKey);
            var result = await api.VerifyLookup(standardized, token, template, token2, token3);

            if (result.Cost == 200)
            {
                response.Success = true;
                response.Code = 200;
                return await Task.FromResult(response);
            }

            response.Success = false;
            response.Code = Convert.ToInt16(result.Cost);
            response.Messages.Add(new Messages { message = result.Message });
            return await Task.FromResult(response);
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Code = 500;
            response.Messages.Add(new Messages { message = ex.Message });
            _logger.LogWarning(ex, "SMS send failed. Template={Template}, Phone={Phone}", template, phoneNumber);
            return await Task.FromResult(response);
        }
    }
}
