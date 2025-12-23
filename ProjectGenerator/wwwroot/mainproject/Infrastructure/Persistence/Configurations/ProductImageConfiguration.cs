using MobiRooz.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MobiRooz.Infrastructure.Persistence.Configurations;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(image => image.Id);

        builder.Property(image => image.ImagePath)
            .IsRequired()
            .HasMaxLength(600);

        builder.Property(image => image.DisplayOrder)
            .HasDefaultValue(0);
    }
}
