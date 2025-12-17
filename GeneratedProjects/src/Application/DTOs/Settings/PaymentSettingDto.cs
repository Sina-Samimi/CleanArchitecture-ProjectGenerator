namespace TestAttarClone.Application.DTOs.Settings;

public sealed record PaymentSettingDto(
    string ZarinPalMerchantId,
    bool ZarinPalIsSandbox,
    bool IsActive);
