using Attar.Domain.Enums;

namespace Attar.Application.DTOs.Financial;

public sealed record FinancialSettingDto(
    decimal SellerProductSharePercentage,
    decimal ValueAddedTaxPercentage,
    decimal PlatformCommissionPercentage,
    decimal AffiliateCommissionPercentage,
    PlatformCommissionCalculationMethod CommissionCalculationMethod);
