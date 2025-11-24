using Arsis.Domain.Entities;
using Arsis.Domain.Entities.Teachers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class TeacherProfileConfiguration : IEntityTypeConfiguration<TeacherProfile>
{
    public void Configure(EntityTypeBuilder<TeacherProfile> builder)
    {
        builder.ToTable("TeacherProfiles");

        builder.Property(profile => profile.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(profile => profile.Degree)
            .HasMaxLength(200);

        builder.Property(profile => profile.Specialty)
            .HasMaxLength(200);

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

        builder.HasIndex(profile => profile.UserId)
            .HasDatabaseName("IX_TeacherProfiles_UserId")
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        builder.HasOne(profile => profile.User)
            .WithMany()
            .HasForeignKey(profile => profile.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
