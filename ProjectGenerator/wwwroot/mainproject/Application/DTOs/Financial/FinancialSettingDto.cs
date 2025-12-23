using MobiRooz.Domain.Enums;

namespace MobiRooz.Application.DTOs.Financial;

public sealed record FinancialSettingDto(
    decimal SellerProductSharePercentage,
    decimal ValueAddedTaxPercentage,
    decimal PlatformCommissionPercentage,
    decimal AffiliateCommissionPercentage,
    PlatformCommissionCalculationMethod CommissionCalculationMethod);
