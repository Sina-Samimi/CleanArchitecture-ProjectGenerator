using System;
using System.Diagnostics.CodeAnalysis;
using MobiRooz.Domain.Base;
using MobiRooz.Domain.Enums;

namespace MobiRooz.Domain.Entities.Settings;

public sealed class FinancialSetting : Entity
{
    public decimal SellerProductSharePercentage { get; private set; }

    public decimal ValueAddedTaxPercentage { get; private set; }

    public decimal PlatformCommissionPercentage { get; private set; }

    public decimal AffiliateCommissionPercentage { get; private set; }

    public PlatformCommissionCalculationMethod CommissionCalculationMethod { get; private set; }

    [SetsRequiredMembers]
    private FinancialSetting()
    {
    }

    [SetsRequiredMembers]
    public FinancialSetting(
        decimal sellerProductSharePercentage,
        decimal valueAddedTaxPercentage,
        decimal platformCommissionPercentage,
        decimal affiliateCommissionPercentage,
        PlatformCommissionCalculationMethod commissionCalculationMethod)
    {
        UpdatePercentages(
            sellerProductSharePercentage,
            valueAddedTaxPercentage,
            platformCommissionPercentage,
            affiliateCommissionPercentage,
            commissionCalculationMethod,
            true);
    }

    public void Update(
        decimal sellerProductSharePercentage,
        decimal valueAddedTaxPercentage,
        decimal platformCommissionPercentage,
        decimal affiliateCommissionPercentage,
        PlatformCommissionCalculationMethod commissionCalculationMethod)
        => UpdatePercentages(
            sellerProductSharePercentage,
            valueAddedTaxPercentage,
            platformCommissionPercentage,
            affiliateCommissionPercentage,
            commissionCalculationMethod,
            false);

    private void UpdatePercentages(
        decimal sellerProductSharePercentage,
        decimal valueAddedTaxPercentage,
        decimal platformCommissionPercentage,
        decimal affiliateCommissionPercentage,
        PlatformCommissionCalculationMethod commissionCalculationMethod,
        bool initializing)
    {
        SellerProductSharePercentage = NormalizePercentage(sellerProductSharePercentage, nameof(sellerProductSharePercentage));
        ValueAddedTaxPercentage = NormalizePercentage(valueAddedTaxPercentage, nameof(valueAddedTaxPercentage));
        PlatformCommissionPercentage = NormalizePercentage(platformCommissionPercentage, nameof(platformCommissionPercentage));
        AffiliateCommissionPercentage = NormalizePercentage(affiliateCommissionPercentage, nameof(affiliateCommissionPercentage));
        CommissionCalculationMethod = commissionCalculationMethod;

        if (!initializing)
        {
            UpdateDate = DateTimeOffset.UtcNow;
        }
    }

    private static decimal NormalizePercentage(decimal value, string argumentName)
    {
        if (value < 0 || value > 100)
        {
            throw new ArgumentOutOfRangeException(argumentName, "Percentage values must be between 0 and 100.");
        }

        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
