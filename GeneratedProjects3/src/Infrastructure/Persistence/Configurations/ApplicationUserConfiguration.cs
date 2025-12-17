using LogTableRenameTest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogTableRenameTest.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.DeactivationReason)
            .HasMaxLength(500);

        builder.Property(user => user.IsActive)
            .HasDefaultValue(true);

        builder.Property(user => user.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(user => user.CreatedOn)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(user => user.LastModifiedOn)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
