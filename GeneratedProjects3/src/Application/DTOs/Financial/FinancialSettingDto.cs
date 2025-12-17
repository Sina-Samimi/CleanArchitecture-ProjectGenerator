using LogTableRenameTest.Domain.Enums;

namespace LogTableRenameTest.Application.DTOs.Financial;

public sealed record FinancialSettingDto(
    decimal SellerProductSharePercentage,
    decimal ValueAddedTaxPercentage,
    decimal PlatformCommissionPercentage,
    decimal AffiliateCommissionPercentage,
    PlatformCommissionCalculationMethod CommissionCalculationMethod);
