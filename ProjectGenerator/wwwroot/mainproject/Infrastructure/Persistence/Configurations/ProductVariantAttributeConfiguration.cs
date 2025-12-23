using MobiRooz.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MobiRooz.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantAttributeConfiguration : IEntityTypeConfiguration<ProductVariantAttribute>
{
    public void Configure(EntityTypeBuilder<ProductVariantAttribute> builder)
    {
        builder.HasKey(attr => attr.Id);

        builder.Property(attr => attr.ProductId)
            .IsRequired();

        builder.Property(attr => attr.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(attr => attr.DisplayOrder)
            .HasDefaultValue(0);

        // Store options as comma-separated string
        builder.Ignore(attr => attr.Options);
        // Access private property by name (EF Core will use reflection internally)
        builder.Property<string>("OptionsString")
            .HasColumnName("Options")
            .HasMaxLength(2000)
            .HasDefaultValue(string.Empty);

        builder.HasOne(attr => attr.Product)
            .WithMany(p => p.VariantAttributes)
            .HasForeignKey(attr => attr.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(attr => attr.ProductId);
    }
}
