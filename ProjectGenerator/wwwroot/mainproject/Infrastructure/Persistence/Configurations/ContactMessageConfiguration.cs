using MobiRooz.Domain.Entities.Contacts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MobiRooz.Infrastructure.Persistence.Configurations;

public sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.UserId)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.Property(message => message.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(message => message.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(message => message.Phone)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(message => message.Subject)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(message => message.Message)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(message => message.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(message => message.ReadByUserId)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.Property(message => message.AdminReply)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(message => message.RepliedByUserId)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.HasIndex(message => message.UserId);
        builder.HasIndex(message => message.Email);
        builder.HasIndex(message => message.IsRead);
        builder.HasIndex(message => message.CreateDate);
    }
}

