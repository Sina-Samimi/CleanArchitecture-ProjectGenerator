using LogsDtoCloneTest.Domain.Entities.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Configurations;

public sealed class TicketReplyConfiguration : IEntityTypeConfiguration<TicketReply>
{
    public void Configure(EntityTypeBuilder<TicketReply> builder)
    {
        builder.ToTable("TicketReplies");

        builder.HasKey(reply => reply.Id);

        builder.HasOne(reply => reply.Ticket)
            .WithMany(ticket => ticket.Replies)
            .HasForeignKey(reply => reply.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(reply => reply.RepliedBy)
            .WithMany()
            .HasForeignKey(reply => reply.RepliedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(reply => reply.TicketId)
            .IsRequired();

        builder.Property(reply => reply.Message)
            .HasMaxLength(5000)
            .IsRequired();

        builder.Property(reply => reply.IsFromAdmin)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(reply => reply.RepliedById)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.HasIndex(reply => reply.TicketId);
        builder.HasIndex(reply => reply.CreateDate);
    }
}
