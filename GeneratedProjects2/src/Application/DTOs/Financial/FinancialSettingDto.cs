using LogsDtoCloneTest.Domain.Enums;

namespace LogsDtoCloneTest.Application.DTOs.Financial;

public sealed record FinancialSettingDto(
    decimal SellerProductSharePercentage,
    decimal ValueAddedTaxPercentage,
    decimal PlatformCommissionPercentage,
    decimal AffiliateCommissionPercentage,
    PlatformCommissionCalculationMethod CommissionCalculationMethod);
