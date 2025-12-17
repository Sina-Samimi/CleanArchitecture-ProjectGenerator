using Attar.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class ShoppingCartItemConfiguration : IEntityTypeConfiguration<ShoppingCartItem>
{
    public void Configure(EntityTypeBuilder<ShoppingCartItem> builder)
    {
        builder.HasKey(item => item.Id);

        builder.Property(item => item.ProductName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(item => item.ProductSlug)
            .HasMaxLength(250);

        builder.Property(item => item.ThumbnailPath)
            .HasMaxLength(600)
            .IsRequired(false);

        builder.Property(item => item.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(item => item.CompareAtPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(item => item.OfferId)
            .IsRequired(false);

        builder.Property(item => item.Quantity)
            .HasDefaultValue(1);

        builder.HasIndex(item => item.CartId);

        builder.HasIndex(item => new { item.CartId, item.ProductId, item.VariantId, item.OfferId })
            .IsUnique();
    }
}
