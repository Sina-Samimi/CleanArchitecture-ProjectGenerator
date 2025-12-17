using Attar.Domain.Entities.Blogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class BlogConfiguration : IEntityTypeConfiguration<Blog>
{
    public void Configure(EntityTypeBuilder<Blog> builder)
    {
        builder.HasKey(blog => blog.Id);

        builder.Property(blog => blog.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(blog => blog.Summary)
            .HasMaxLength(600)
            .IsRequired(false);

        builder.Property(blog => blog.Content)
            .IsRequired();

        builder.Property(blog => blog.SeoTitle)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(blog => blog.SeoDescription)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(blog => blog.SeoKeywords)
            .HasMaxLength(400)
            .IsRequired(false);

        builder.Property(blog => blog.SeoSlug)
            .HasMaxLength(250)
            .IsRequired(false);

        builder.HasIndex(blog => blog.SeoSlug);

        builder.Property(blog => blog.Robots)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(blog => blog.FeaturedImagePath)
            .HasMaxLength(600)
            .IsRequired(false);

        builder.Property(blog => blog.TagList)
            .HasMaxLength(1000)
            .HasColumnName("Tags")
            .IsRequired(false);

        builder.Property(blog => blog.LikeCount)
            .HasDefaultValue(0);

        builder.Property(blog => blog.DislikeCount)
            .HasDefaultValue(0);

        builder.HasOne(blog => blog.Category)
            .WithMany()
            .HasForeignKey(blog => blog.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(blog => blog.Author)
            .WithMany()
            .HasForeignKey(blog => blog.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(blog => blog.Comments)
            .WithOne(comment => comment.Blog)
            .HasForeignKey(comment => comment.BlogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(blog => blog.Views)
            .WithOne(view => view.Blog)
            .HasForeignKey(view => view.BlogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(blog => blog.Comments)
            .HasField("_comments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(blog => blog.Views)
            .HasField("_views")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(blog => blog.Tags);

    }
}
