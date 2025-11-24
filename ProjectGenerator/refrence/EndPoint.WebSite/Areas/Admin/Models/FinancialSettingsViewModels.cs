using System.ComponentModel.DataAnnotations;
using Arsis.Application.Commands.Admin.FinancialSettings;
using Arsis.Application.DTOs.Financial;

namespace EndPoint.WebSite.Areas.Admin.Models;

public sealed class FinancialSettingsViewModel
{
    private const string PercentageValidationMessage = "درصد باید مقداری بین ۰ تا ۱۰۰ باشد.";

    [Display(Name = "سهم مدرس از فروش پکیج‌ها (%)")]
    [Range(0, 100, ErrorMessage = PercentageValidationMessage)]
    [DisplayFormat(DataFormatString = "{0:0.##}", ApplyFormatInEditMode = true)]
    public decimal TeacherPackageSharePercentage { get; set; }

    [Display(Name = "سهم مدرس از رویدادهای زنده (%)")]
    [Range(0, 100, ErrorMessage = PercentageValidationMessage)]
    [DisplayFormat(DataFormatString = "{0:0.##}", ApplyFormatInEditMode = true)]
    public decimal TeacherLiveEventSharePercentage { get; set; }

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

    public static FinancialSettingsViewModel FromDto(FinancialSettingDto dto)
        => new()
        {
            TeacherPackageSharePercentage = dto.TeacherPackageSharePercentage,
            TeacherLiveEventSharePercentage = dto.TeacherLiveEventSharePercentage,
            ValueAddedTaxPercentage = dto.ValueAddedTaxPercentage,
            PlatformCommissionPercentage = dto.PlatformCommissionPercentage,
            AffiliateCommissionPercentage = dto.AffiliateCommissionPercentage
        };

    public UpdateFinancialSettingsCommand ToCommand()
        => new(
            TeacherPackageSharePercentage,
            TeacherLiveEventSharePercentage,
            ValueAddedTaxPercentage,
            PlatformCommissionPercentage,
            AffiliateCommissionPercentage);
}
