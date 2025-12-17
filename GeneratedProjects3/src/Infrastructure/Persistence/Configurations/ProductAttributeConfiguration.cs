using LogTableRenameTest.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogTableRenameTest.Infrastructure.Persistence.Configurations;

public sealed class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.HasKey(attribute => attribute.Id);

        builder.Property(attribute => attribute.ProductId)
            .IsRequired();

        builder.Property(attribute => attribute.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(attribute => attribute.Value)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(attribute => attribute.DisplayOrder)
            .HasDefaultValue(0);

        builder.HasOne(attribute => attribute.Product)
            .WithMany(product => product.Attributes)
            .HasForeignKey(attribute => attribute.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(attribute => attribute.ProductId);
    }
}

