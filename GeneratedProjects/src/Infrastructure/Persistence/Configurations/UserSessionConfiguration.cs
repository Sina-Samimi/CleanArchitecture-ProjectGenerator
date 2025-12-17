using TestAttarClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TestAttarClone.Infrastructure.Persistence.Configurations;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");

        builder.HasKey(session => session.Id);

        builder.Property(session => session.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(session => session.DeviceType)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(session => session.ClientName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(session => session.UserAgent)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(session => session.SignedInAt)
            .IsRequired();

        builder.Property(session => session.LastSeenAt)
            .IsRequired();

        builder.HasIndex(session => session.UserId);

        builder.HasOne(session => session.User)
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
