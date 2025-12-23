using MobiRooz.Domain.Entities;
using MobiRooz.Domain.Entities.Blogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MobiRooz.Infrastructure.Persistence.Configurations;

public sealed class BlogAuthorConfiguration : IEntityTypeConfiguration<BlogAuthor>
{
    public void Configure(EntityTypeBuilder<BlogAuthor> builder)
    {
        builder.HasKey(author => author.Id);

        builder.Property(author => author.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(author => author.Bio)
            .HasMaxLength(1000);

        builder.Property(author => author.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(author => author.IsActive)
            .HasDefaultValue(true);

        builder.Property(author => author.UserId)
            .HasMaxLength(450);

        builder.HasOne(author => author.User)
            .WithMany()
            .HasForeignKey(author => author.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
