using Attar.Application.Commands.Admin.PaymentSettings;
using Attar.Application.DTOs.Settings;
using System.ComponentModel.DataAnnotations;

namespace Attar.WebSite.Areas.Admin.Models;

public sealed class PaymentSettingsViewModel
{
    [Required(ErrorMessage = "Merchant ID الزامی است.")]
    [Display(Name = "ZarinPal Merchant ID")]
    [StringLength(500, ErrorMessage = "Merchant ID نمی‌تواند بیشتر از ۵۰۰ کاراکتر باشد.")]
    public string ZarinPalMerchantId { get; set; } = string.Empty;

    [Display(Name = "حالت Sandbox")]
    public bool ZarinPalIsSandbox { get; set; }

    [Display(Name = "فعال")]
    public bool IsActive { get; set; }

    public static PaymentSettingsViewModel FromDto(PaymentSettingDto dto)
    {
        return new PaymentSettingsViewModel
        {
            ZarinPalMerchantId = dto.ZarinPalMerchantId,
            ZarinPalIsSandbox = dto.ZarinPalIsSandbox,
            IsActive = dto.IsActive
        };
    }

    public UpdatePaymentSettingsCommand ToCommand()
    {
        return new UpdatePaymentSettingsCommand(
            ZarinPalMerchantId,
            ZarinPalIsSandbox,
            IsActive);
    }
}
