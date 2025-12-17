using System.Net;
using LogTableRenameTest.Domain.Entities.Visits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LogTableRenameTest.Infrastructure.Persistence.Configurations;

public sealed class SiteVisitConfiguration : IEntityTypeConfiguration<SiteVisit>
{
    public void Configure(EntityTypeBuilder<SiteVisit> builder)
    {
        builder.ToTable("SiteVisits");

        builder.HasKey(visit => visit.Id);

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

        builder.HasIndex(visit => visit.VisitDate);

        builder.HasIndex(visit => new { visit.VisitDate, visit.ViewerIp })
            .IsUnique();
    }
}

