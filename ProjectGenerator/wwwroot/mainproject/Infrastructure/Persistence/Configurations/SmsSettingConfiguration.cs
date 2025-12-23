using MobiRooz.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MobiRooz.Infrastructure.Persistence.Configurations;

public sealed class SmsSettingConfiguration : IEntityTypeConfiguration<SmsSetting>
{
    public void Configure(EntityTypeBuilder<SmsSetting> builder)
    {
        builder.ToTable("SmsSettings");

        builder.Property(setting => setting.ApiKey)
            .IsRequired()
            .HasMaxLength(500);
    }
}
