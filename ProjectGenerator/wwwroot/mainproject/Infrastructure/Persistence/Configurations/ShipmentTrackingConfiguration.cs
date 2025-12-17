using Attar.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class ShipmentTrackingConfiguration : IEntityTypeConfiguration<ShipmentTracking>
{
    public void Configure(EntityTypeBuilder<ShipmentTracking> builder)
    {
        builder.HasKey(tracking => tracking.Id);

        builder.Property(tracking => tracking.InvoiceItemId)
            .IsRequired();

        builder.Property(tracking => tracking.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(tracking => tracking.TrackingNumber)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(tracking => tracking.Notes)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(tracking => tracking.StatusDate)
            .IsRequired();

        builder.Property(tracking => tracking.UpdatedById)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.HasOne(tracking => tracking.InvoiceItem)
            .WithMany()
            .HasForeignKey(tracking => tracking.InvoiceItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tracking => tracking.UpdatedBy)
            .WithMany()
            .HasForeignKey(tracking => tracking.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(tracking => tracking.InvoiceItemId);
        builder.HasIndex(tracking => tracking.Status);
        builder.HasIndex(tracking => tracking.StatusDate);
    }
}

