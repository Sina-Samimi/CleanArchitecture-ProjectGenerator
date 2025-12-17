using System;

namespace LogTableRenameTest.WebSite.Services;

public interface IPhoneVerificationService
{
    PhoneVerificationCode GenerateCode(string phoneNumber);

    PhoneVerificationValidationResult ValidateCode(string phoneNumber, string code);

    PhoneVerificationState? GetState(string phoneNumber);

    void ClearCode(string phoneNumber);

    TimeSpan CodeLifetime { get; }
}
