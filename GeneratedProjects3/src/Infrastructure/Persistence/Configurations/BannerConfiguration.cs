using LogTableRenameTest.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogTableRenameTest.Infrastructure.Persistence.Configurations;

public sealed class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.ToTable("Banners");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.ImagePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.LinkUrl)
            .HasMaxLength(1000);

        builder.Property(b => b.AltText)
            .HasMaxLength(200);

        builder.Property(b => b.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(b => b.ShowOnHomePage)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(b => b.StartDate)
            .HasColumnType("datetimeoffset");

        builder.Property(b => b.EndDate)
            .HasColumnType("datetimeoffset");

        builder.HasIndex(b => b.DisplayOrder);
        builder.HasIndex(b => b.IsActive);
        builder.HasIndex(b => b.ShowOnHomePage);
    }
}

