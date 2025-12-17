using LogsDtoCloneTest.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Configurations;

public sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.ToTable("SiteSettings");

        builder.Property(setting => setting.SiteTitle)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(setting => setting.SiteEmail)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(setting => setting.SupportEmail)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(setting => setting.ContactPhone)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(setting => setting.SupportPhone)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(setting => setting.Address)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(setting => setting.ContactDescription)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(setting => setting.LogoPath)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(setting => setting.FaviconPath)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(setting => setting.ShortDescription)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(setting => setting.TermsAndConditions)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(setting => setting.HeroBannerPath)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(setting => setting.TelegramUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(setting => setting.InstagramUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(setting => setting.WhatsAppUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(setting => setting.LinkedInUrl)
            .HasMaxLength(500)
            .IsRequired(false);
    }
}
