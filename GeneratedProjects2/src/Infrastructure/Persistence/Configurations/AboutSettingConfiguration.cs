using LogsDtoCloneTest.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Configurations;

public sealed class AboutSettingConfiguration : IEntityTypeConfiguration<AboutSetting>
{
    public void Configure(EntityTypeBuilder<AboutSetting> builder)
    {
        builder.ToTable("AboutSettings");

        builder.Property(setting => setting.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(setting => setting.Description)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(setting => setting.Vision)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(setting => setting.Mission)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(setting => setting.ImagePath)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(setting => setting.MetaTitle)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(setting => setting.MetaDescription)
            .HasMaxLength(500)
            .IsRequired(false);
    }
}

