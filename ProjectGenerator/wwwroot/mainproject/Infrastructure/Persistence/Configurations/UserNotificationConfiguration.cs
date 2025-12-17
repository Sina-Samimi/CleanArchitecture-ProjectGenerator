using Attar.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.HasKey(userNotification => userNotification.Id);

        builder.Property(userNotification => userNotification.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(userNotification => userNotification.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne(userNotification => userNotification.Notification)
            .WithMany(notification => notification.UserNotifications)
            .HasForeignKey(userNotification => userNotification.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(userNotification => userNotification.User)
            .WithMany()
            .HasForeignKey(userNotification => userNotification.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(userNotification => new { userNotification.UserId, userNotification.NotificationId })
            .IsUnique();

        builder.HasIndex(userNotification => new { userNotification.UserId, userNotification.IsRead });
    }
}

