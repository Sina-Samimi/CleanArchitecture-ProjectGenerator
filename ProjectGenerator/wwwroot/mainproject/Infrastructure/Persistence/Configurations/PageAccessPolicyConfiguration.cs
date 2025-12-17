using Attar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class PageAccessPolicyConfiguration : IEntityTypeConfiguration<PageAccessPolicy>
{
    public void Configure(EntityTypeBuilder<PageAccessPolicy> builder)
    {
        builder.ToTable("PageAccessPolicies");

        builder.Property(policy => policy.Area)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue(string.Empty);

        builder.Property(policy => policy.Controller)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(policy => policy.Action)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(policy => policy.PermissionKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(policy => new { policy.Area, policy.Controller, policy.Action });
        builder.HasIndex(policy => new { policy.Area, policy.Controller, policy.Action, policy.PermissionKey })
            .IsUnique();
    }
}
