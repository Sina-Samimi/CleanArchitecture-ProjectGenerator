using Attar.Domain.Entities.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(ticket => ticket.Id);

        builder.HasOne(ticket => ticket.User)
            .WithMany()
            .HasForeignKey(ticket => ticket.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ticket => ticket.AssignedTo)
            .WithMany()
            .HasForeignKey(ticket => ticket.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ticket => ticket.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(ticket => ticket.Subject)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(ticket => ticket.Message)
            .HasMaxLength(5000)
            .IsRequired();

        builder.Property(ticket => ticket.Department)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(ticket => ticket.AttachmentPath)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(ticket => ticket.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ticket => ticket.AssignedToId)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.Property(ticket => ticket.HasUnreadReplies)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasMany(ticket => ticket.Replies)
            .WithOne(reply => reply.Ticket)
            .HasForeignKey(reply => reply.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(ticket => ticket.Replies)
            .HasField("_replies")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(ticket => ticket.UserId);
        builder.HasIndex(ticket => ticket.Status);
        builder.HasIndex(ticket => ticket.AssignedToId);
        builder.HasIndex(ticket => ticket.CreateDate);
        builder.HasIndex(ticket => ticket.HasUnreadReplies);
    }
}
