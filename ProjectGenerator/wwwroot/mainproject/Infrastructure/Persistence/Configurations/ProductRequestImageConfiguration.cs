using Attar.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class ProductRequestImageConfiguration : IEntityTypeConfiguration<ProductRequestImage>
{
    public void Configure(EntityTypeBuilder<ProductRequestImage> builder)
    {
        builder.ToTable("ProductRequestImages");

        builder.HasKey(image => image.Id);

        builder.Property(image => image.Path)
            .IsRequired()
            .HasMaxLength(600);

        builder.Property(image => image.Order)
            .HasDefaultValue(0);

        builder.Property(image => image.ProductRequestId)
            .IsRequired();
    }
}

