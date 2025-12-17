using LogTableRenameTest.Domain.Entities.Seo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogTableRenameTest.Infrastructure.Persistence.Configurations;

public sealed class SeoOgImageConfiguration : IEntityTypeConfiguration<SeoOgImage>
{
    public void Configure(EntityTypeBuilder<SeoOgImage> builder)
    {
        builder.ToTable("SeoOgImages");

        builder.HasKey(image => image.Id);

        builder.Property(image => image.SeoMetadataId)
            .IsRequired();

        builder.Property(image => image.ImageUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(image => image.Width)
            .IsRequired(false);

        builder.Property(image => image.Height)
            .IsRequired(false);

        builder.Property(image => image.ImageType)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(image => image.Alt)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(image => image.DisplayOrder)
            .HasDefaultValue(0);

        builder.HasOne(image => image.SeoMetadata)
            .WithMany()
            .HasForeignKey(image => image.SeoMetadataId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(image => new { image.SeoMetadataId, image.DisplayOrder });
    }
}

