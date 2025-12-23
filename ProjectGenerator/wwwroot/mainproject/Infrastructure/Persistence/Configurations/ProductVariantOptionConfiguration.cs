using MobiRooz.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MobiRooz.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantOptionConfiguration : IEntityTypeConfiguration<ProductVariantOption>
{
    public void Configure(EntityTypeBuilder<ProductVariantOption> builder)
    {
        builder.HasKey(opt => opt.Id);

        builder.Property(opt => opt.VariantId)
            .IsRequired();

        builder.Property(opt => opt.VariantAttributeId)
            .IsRequired();

        builder.Property(opt => opt.Value)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne(opt => opt.Variant)
            .WithMany(v => v.Options)
            .HasForeignKey(opt => opt.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(opt => opt.VariantId);
        builder.HasIndex(opt => opt.VariantAttributeId);
    }
}
