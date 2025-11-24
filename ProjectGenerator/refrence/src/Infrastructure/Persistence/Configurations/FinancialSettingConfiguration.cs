using Arsis.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class FinancialSettingConfiguration : IEntityTypeConfiguration<FinancialSetting>
{
    public void Configure(EntityTypeBuilder<FinancialSetting> builder)
    {
        builder.ToTable("FinancialSettings");

        builder.Property(setting => setting.TeacherPackageSharePercentage)
            .HasPrecision(5, 2);

        builder.Property(setting => setting.TeacherLiveEventSharePercentage)
            .HasPrecision(5, 2);

        builder.Property(setting => setting.ValueAddedTaxPercentage)
            .HasPrecision(5, 2);

        builder.Property(setting => setting.PlatformCommissionPercentage)
            .HasPrecision(5, 2);

        builder.Property(setting => setting.AffiliateCommissionPercentage)
            .HasPrecision(5, 2);
    }
}
