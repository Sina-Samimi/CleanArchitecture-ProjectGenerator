using Arsis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(o => o.Code)
            .IsUnique();

        builder.Property(o => o.Description)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(o => o.AdminName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.AdminEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(o => o.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(o => o.Address)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasDefaultValue(OrganizationStatus.Active);

        builder.Property(o => o.MaxUsers)
            .IsRequired()
            .HasDefaultValue(100);

        builder.Property(o => o.SubscriptionExpiry)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(o => o.Name);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.AdminEmail);
    }
}
