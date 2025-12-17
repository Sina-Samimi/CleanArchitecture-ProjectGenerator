using TestAttarClone.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TestAttarClone.Infrastructure.Persistence.Configurations;

public sealed class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.HasKey(cart => cart.Id);

        builder.Property(cart => cart.UserId)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.Property(cart => cart.AppliedDiscountCode)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(cart => cart.AppliedDiscountValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(cart => cart.AppliedDiscountAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(cart => cart.DiscountOriginalSubtotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(cart => cart.AppliedDiscountWasCapped)
            .HasDefaultValue(false);

        builder.HasIndex(cart => cart.UserId)
            .HasFilter("[UserId] IS NOT NULL");

        builder.HasIndex(cart => cart.AnonymousId)
            .HasFilter("[AnonymousId] IS NOT NULL");

        builder.HasMany(cart => cart.Items)
            .WithOne(item => item.Cart)
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        var itemsNavigation = builder.Metadata.FindNavigation(nameof(ShoppingCart.Items));
        itemsNavigation?.SetField("_items");
        itemsNavigation?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
