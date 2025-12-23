using MobiRooz.Domain.Entities;
using MobiRooz.Domain.Entities.Sellers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MobiRooz.Infrastructure.Persistence.Configurations;

public sealed class SellerProfileConfiguration : IEntityTypeConfiguration<SellerProfile>
{
    public void Configure(EntityTypeBuilder<SellerProfile> builder)
    {
        builder.ToTable("SellerProfiles");

        builder.Property(profile => profile.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(profile => profile.LicenseNumber)
            .HasMaxLength(100);

        builder.Property(profile => profile.LicenseIssueDate)
            .HasColumnType("date");

        builder.Property(profile => profile.LicenseExpiryDate)
            .HasColumnType("date");

        builder.Property(profile => profile.ShopAddress)
            .HasMaxLength(500);

        builder.Property(profile => profile.WorkingHours)
            .HasMaxLength(200);

        builder.Property(profile => profile.ExperienceYears);

        builder.Property(profile => profile.Bio)
            .HasMaxLength(2000);

        builder.Property(profile => profile.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(profile => profile.ContactEmail)
            .HasMaxLength(200);

        builder.Property(profile => profile.ContactPhone)
            .HasMaxLength(50);

        builder.Property(profile => profile.UserId)
            .HasMaxLength(450);

        builder.Property(profile => profile.SellerSharePercentage)
            .HasColumnType("decimal(5,2)");

        builder.HasIndex(profile => profile.UserId)
            .HasDatabaseName("IX_SellerProfiles_UserId")
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        builder.HasOne(profile => profile.User)
            .WithMany()
            .HasForeignKey(profile => profile.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
