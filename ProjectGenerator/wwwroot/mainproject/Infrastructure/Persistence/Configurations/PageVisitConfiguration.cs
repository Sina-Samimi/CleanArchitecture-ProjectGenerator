using System.Net;
using Attar.Domain.Entities.Visits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class PageVisitConfiguration : IEntityTypeConfiguration<PageVisit>
{
    public void Configure(EntityTypeBuilder<PageVisit> builder)
    {
        builder.ToTable("PageVisits");

        builder.HasKey(visit => visit.Id);

        builder.Property(visit => visit.PageId)
            .IsRequired(false);

        builder.HasOne(visit => visit.Page)
            .WithMany()
            .HasForeignKey(visit => visit.PageId)
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

        builder.HasIndex(visit => visit.PageId);
        builder.HasIndex(visit => visit.VisitDate);

        builder.HasIndex(visit => new { visit.PageId, visit.VisitDate, visit.ViewerIp })
            .IsUnique();
    }
}

