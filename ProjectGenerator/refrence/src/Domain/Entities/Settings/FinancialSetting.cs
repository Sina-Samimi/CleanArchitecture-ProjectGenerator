using System;
using System.Diagnostics.CodeAnalysis;
using Arsis.Domain.Base;

namespace Arsis.Domain.Entities.Settings;

public sealed class FinancialSetting : Entity
{
    public decimal TeacherPackageSharePercentage { get; private set; }

    public decimal TeacherLiveEventSharePercentage { get; private set; }

    public decimal ValueAddedTaxPercentage { get; private set; }

    public decimal PlatformCommissionPercentage { get; private set; }

    public decimal AffiliateCommissionPercentage { get; private set; }

    [SetsRequiredMembers]
    private FinancialSetting()
    {
    }

    [SetsRequiredMembers]
    public FinancialSetting(
        decimal teacherPackageSharePercentage,
        decimal teacherLiveEventSharePercentage,
        decimal valueAddedTaxPercentage,
        decimal platformCommissionPercentage,
        decimal affiliateCommissionPercentage)
    {
        UpdatePercentages(
            teacherPackageSharePercentage,
            teacherLiveEventSharePercentage,
            valueAddedTaxPercentage,
            platformCommissionPercentage,
            affiliateCommissionPercentage,
            true);
    }

    public void Update(
        decimal teacherPackageSharePercentage,
        decimal teacherLiveEventSharePercentage,
        decimal valueAddedTaxPercentage,
        decimal platformCommissionPercentage,
        decimal affiliateCommissionPercentage)
        => UpdatePercentages(
            teacherPackageSharePercentage,
            teacherLiveEventSharePercentage,
            valueAddedTaxPercentage,
            platformCommissionPercentage,
            affiliateCommissionPercentage,
            false);

    private void UpdatePercentages(
        decimal teacherPackageSharePercentage,
        decimal teacherLiveEventSharePercentage,
        decimal valueAddedTaxPercentage,
        decimal platformCommissionPercentage,
        decimal affiliateCommissionPercentage,
        bool initializing)
    {
        TeacherPackageSharePercentage = NormalizePercentage(teacherPackageSharePercentage, nameof(teacherPackageSharePercentage));
        TeacherLiveEventSharePercentage = NormalizePercentage(teacherLiveEventSharePercentage, nameof(teacherLiveEventSharePercentage));
        ValueAddedTaxPercentage = NormalizePercentage(valueAddedTaxPercentage, nameof(valueAddedTaxPercentage));
        PlatformCommissionPercentage = NormalizePercentage(platformCommissionPercentage, nameof(platformCommissionPercentage));
        AffiliateCommissionPercentage = NormalizePercentage(affiliateCommissionPercentage, nameof(affiliateCommissionPercentage));

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
