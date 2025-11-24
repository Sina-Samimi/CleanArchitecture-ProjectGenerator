namespace Arsis.Application.DTOs.Financial;

public sealed record FinancialSettingDto(
    decimal TeacherPackageSharePercentage,
    decimal TeacherLiveEventSharePercentage,
    decimal ValueAddedTaxPercentage,
    decimal PlatformCommissionPercentage,
    decimal AffiliateCommissionPercentage);
