using TestAttarClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TestAttarClone.Infrastructure.Persistence.Configurations;

public sealed class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.HasKey(address => address.Id);

        builder.Property(address => address.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(address => address.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(address => address.RecipientName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(address => address.RecipientPhone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(address => address.Province)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(address => address.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(address => address.PostalCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(address => address.AddressLine)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(address => address.Plaque)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(address => address.Unit)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(address => address.IsDefault)
            .HasDefaultValue(false);

        builder.Property(address => address.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne(address => address.User)
            .WithMany()
            .HasForeignKey(address => address.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(address => address.UserId);
        builder.HasIndex(address => new { address.UserId, address.IsDefault });
    }
}
