using TestAttarClone.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TestAttarClone.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.ProductId)
            .IsRequired();

        builder.Property(v => v.Price)
            .HasPrecision(18, 2);

        builder.Property(v => v.CompareAtPrice)
            .HasPrecision(18, 2);

        builder.Property(v => v.StockQuantity)
            .HasDefaultValue(0);

        builder.Property(v => v.Sku)
            .HasMaxLength(100);

        builder.Property(v => v.ImagePath)
            .HasMaxLength(600);

        builder.Property(v => v.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(v => v.Product)
            .WithMany()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Options)
            .WithOne(o => o.Variant)
            .HasForeignKey(o => o.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.ProductId);
        builder.HasIndex(v => v.Sku)
            .IsUnique()
            .HasFilter("[Sku] IS NOT NULL");
    }
}
