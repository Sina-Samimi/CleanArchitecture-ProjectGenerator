using TestAttarClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TestAttarClone.Infrastructure.Persistence.Configurations;

public sealed class AttarApplicationLogConfiguration : IEntityTypeConfiguration<AttarApplicationLog>
{
    public void Configure(EntityTypeBuilder<AttarApplicationLog> builder)
    {
        builder.ToTable("AttarApplicationLogs", schema: "Logs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.Level)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(log => log.Message)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(log => log.Exception)
            .HasColumnType("nvarchar(max)");

        builder.Property(log => log.SourceContext)
            .HasMaxLength(500);

        builder.Property(log => log.Properties)
            .HasColumnType("nvarchar(max)");

        builder.Property(log => log.RequestPath)
            .HasMaxLength(1000);

        builder.Property(log => log.RequestMethod)
            .HasMaxLength(10);

        builder.Property(log => log.UserAgent)
            .HasMaxLength(500);

        builder.Property(log => log.RemoteIpAddress)
            .HasMaxLength(64);

        builder.Property(log => log.ApplicationName)
            .HasMaxLength(200);

        builder.Property(log => log.MachineName)
            .HasMaxLength(200);

        builder.Property(log => log.Environment)
            .HasMaxLength(50);

        // Indexes for better query performance
        builder.HasIndex(log => log.Level);
        builder.HasIndex(log => log.CreateDate);
        builder.HasIndex(log => new { log.Level, log.CreateDate });
        builder.HasIndex(log => log.ApplicationName);
        builder.HasIndex(log => log.SourceContext);
    }
}

