using Attar.Domain.Entities.Settings;
using Attar.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class FinancialSettingConfiguration : IEntityTypeConfiguration<FinancialSetting>
{
    public void Configure(EntityTypeBuilder<FinancialSetting> builder)
    {
        builder.ToTable("FinancialSettings");

        builder.Property(setting => setting.SellerProductSharePercentage)
            .HasPrecision(5, 2);

        builder.Property(setting => setting.ValueAddedTaxPercentage)
            .HasPrecision(5, 2);

        builder.Property(setting => setting.PlatformCommissionPercentage)
            .HasPrecision(5, 2);

        builder.Property(setting => setting.AffiliateCommissionPercentage)
            .HasPrecision(5, 2);

        builder.Property(setting => setting.CommissionCalculationMethod)
            .HasConversion<int>()
            .HasDefaultValue(PlatformCommissionCalculationMethod.Complementary);
    }
}
