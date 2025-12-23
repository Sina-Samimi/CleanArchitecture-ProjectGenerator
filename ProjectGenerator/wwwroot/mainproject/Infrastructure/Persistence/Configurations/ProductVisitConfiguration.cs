using System.Net;
using MobiRooz.Domain.Entities.Visits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MobiRooz.Infrastructure.Persistence.Configurations;

public sealed class ProductVisitConfiguration : IEntityTypeConfiguration<ProductVisit>
{
    public void Configure(EntityTypeBuilder<ProductVisit> builder)
    {
        builder.ToTable("ProductVisits");

        builder.HasKey(visit => visit.Id);

        builder.Property(visit => visit.ProductId)
            .IsRequired(false);

        builder.HasOne(visit => visit.Product)
            .WithMany()
            .HasForeignKey(visit => visit.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        var ipConverter = new ValueConverter<IPAddress, string>(
            address => address.ToString(),
            value => string.IsNullOrWhiteSpace(value) ? IPAddress.None : IPAddress.Parse(value));

        builder.Property(visit => visit.ViewerIp)
            .HasConversion(ipConverter)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(visit => visit.VisitDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(visit => visit.UserAgent)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(visit => visit.Referrer)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.HasIndex(visit => visit.ProductId);
        builder.HasIndex(visit => visit.VisitDate);

        builder.HasIndex(visit => new { visit.ProductId, visit.VisitDate, visit.ViewerIp })
            .IsUnique();
    }
}

