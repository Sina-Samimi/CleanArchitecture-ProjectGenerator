using Attar.Domain.Entities.Blogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class BlogCommentConfiguration : IEntityTypeConfiguration<BlogComment>
{
    public void Configure(EntityTypeBuilder<BlogComment> builder)
    {
        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.AuthorName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(comment => comment.AuthorEmail)
            .HasMaxLength(200);

        builder.Property(comment => comment.Content)
            .IsRequired();

        builder.Property(comment => comment.IsApproved)
            .HasDefaultValue(true);

        builder.Property(comment => comment.ApprovedById)
            .HasMaxLength(450);

        builder.Property(comment => comment.ApprovedAt);

        builder.HasOne(comment => comment.Parent)
            .WithMany()
            .HasForeignKey(comment => comment.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(comment => comment.ApprovedBy)
            .WithMany()
            .HasForeignKey(comment => comment.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
