using MobiRooz.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MobiRooz.Infrastructure.Persistence.Configurations;

public sealed class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(item => item.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(item => item.ItemType)
            .HasConversion<int>();

        builder.Property(item => item.Quantity)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(1);

        builder.Property(item => item.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(item => item.DiscountAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(item => item.ReferenceId)
            .IsRequired(false);

        builder.Property(item => item.VariantId)
            .IsRequired(false);

        builder.HasMany(item => item.Attributes)
            .WithOne(attribute => attribute.InvoiceItem)
            .HasForeignKey(attribute => attribute.InvoiceItemId)
            .OnDelete(DeleteBehavior.Cascade);

        if (builder.Metadata.FindNavigation(nameof(InvoiceItem.Attributes)) is { } attributesNavigation)
        {
            attributesNavigation.SetField("_attributes");
            attributesNavigation.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
