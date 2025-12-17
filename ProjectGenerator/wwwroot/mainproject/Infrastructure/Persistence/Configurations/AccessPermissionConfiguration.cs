using Attar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class AccessPermissionConfiguration : IEntityTypeConfiguration<AccessPermission>
{
    public void Configure(EntityTypeBuilder<AccessPermission> builder)
    {
        builder.ToTable("AccessPermissions");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Key)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(permission => permission.Key)
            .IsUnique();

        builder.Property(permission => permission.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(permission => permission.Description)
            .HasMaxLength(1024);

        builder.Property(permission => permission.IsCore)
            .IsRequired();

        builder.Property(permission => permission.GroupKey)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(permission => permission.GroupKey);

        builder.Property(permission => permission.GroupDisplayName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(permission => permission.CreatedAt)
            .IsRequired();
    }
}
