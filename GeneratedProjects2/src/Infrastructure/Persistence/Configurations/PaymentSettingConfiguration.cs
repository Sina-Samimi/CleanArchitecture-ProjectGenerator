using LogsDtoCloneTest.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Configurations;

public sealed class PaymentSettingConfiguration : IEntityTypeConfiguration<PaymentSetting>
{
    public void Configure(EntityTypeBuilder<PaymentSetting> builder)
    {
        builder.ToTable("PaymentSettings");

        builder.Property(setting => setting.ZarinPalMerchantId)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(setting => setting.ZarinPalIsSandbox)
            .IsRequired();

        builder.Property(setting => setting.IsActive)
            .IsRequired();
    }
}
