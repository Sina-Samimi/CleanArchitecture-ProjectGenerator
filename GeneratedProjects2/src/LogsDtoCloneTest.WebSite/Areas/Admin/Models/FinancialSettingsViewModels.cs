using System.ComponentModel.DataAnnotations;
using LogsDtoCloneTest.Application.Commands.Admin.FinancialSettings;
using LogsDtoCloneTest.Application.DTOs.Financial;
using LogsDtoCloneTest.Domain.Enums;

namespace LogsDtoCloneTest.WebSite.Areas.Admin.Models;

public sealed class FinancialSettingsViewModel
{
    private const string PercentageValidationMessage = "درصد باید مقداری بین ۰ تا ۱۰۰ باشد.";

    [Display(Name = "سهم فروشنده از فروش محصول (%)")]
    [Range(0, 100, ErrorMessage = PercentageValidationMessage)]
    [DisplayFormat(DataFormatString = "{0:0.##}", ApplyFormatInEditMode = true)]
    public decimal SellerProductSharePercentage { get; set; }

    [Display(Name = "مالیات بر ارزش افزوده (%)")]
    [Range(0, 100, ErrorMessage = PercentageValidationMessage)]
    [DisplayFormat(DataFormatString = "{0:0.##}", ApplyFormatInEditMode = true)]
    public decimal ValueAddedTaxPercentage { get; set; }

    [Display(Name = "کارمزد پلتفرم (%)")]
    [Range(0, 100, ErrorMessage = PercentageValidationMessage)]
    [DisplayFormat(DataFormatString = "{0:0.##}", ApplyFormatInEditMode = true)]
    public decimal PlatformCommissionPercentage { get; set; }

    [Display(Name = "کمیسیون همکاری در فروش (%)")]
    [Range(0, 100, ErrorMessage = PercentageValidationMessage)]
    [DisplayFormat(DataFormatString = "{0:0.##}", ApplyFormatInEditMode = true)]
    public decimal AffiliateCommissionPercentage { get; set; }

    [Display(Name = "نحوه محاسبه کارمزد پلتفرم")]
    [Required(ErrorMessage = "لطفاً نحوه محاسبه کارمزد پلتفرم را انتخاب کنید.")]
    public PlatformCommissionCalculationMethod CommissionCalculationMethod { get; set; } = PlatformCommissionCalculationMethod.Complementary;

    public static FinancialSettingsViewModel FromDto(FinancialSettingDto dto)
        => new()
        {
            SellerProductSharePercentage = dto.SellerProductSharePercentage,
            ValueAddedTaxPercentage = dto.ValueAddedTaxPercentage,
            PlatformCommissionPercentage = dto.PlatformCommissionPercentage,
            AffiliateCommissionPercentage = dto.AffiliateCommissionPercentage,
            CommissionCalculationMethod = dto.CommissionCalculationMethod
        };

    public UpdateFinancialSettingsCommand ToCommand()
        => new(
            SellerProductSharePercentage,
            ValueAddedTaxPercentage,
            PlatformCommissionPercentage,
            AffiliateCommissionPercentage,
            CommissionCalculationMethod);
}
