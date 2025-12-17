using Attar.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(notification => notification.Message)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(notification => notification.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(notification => notification.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(notification => notification.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(notification => notification.SentAt)
            .IsRequired();

        builder.HasOne(notification => notification.CreatedBy)
            .WithMany()
            .HasForeignKey(notification => notification.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(notification => notification.UserNotifications)
            .WithOne(userNotification => userNotification.Notification)
            .HasForeignKey(userNotification => userNotification.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(notification => notification.UserNotifications)
            .HasField("_userNotifications")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

