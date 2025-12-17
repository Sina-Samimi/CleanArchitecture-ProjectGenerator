using Attar.Domain.Entities.Blogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class BlogCategoryConfiguration : IEntityTypeConfiguration<BlogCategory>
{
    public void Configure(EntityTypeBuilder<BlogCategory> builder)
    {
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(category => category.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(category => category.Description)
            .HasMaxLength(500);

        builder.HasIndex(category => category.Slug)
            .IsUnique();

        builder.HasOne(category => category.Parent)
            .WithMany(parent => parent.Children)
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        var childrenNavigation = builder.Metadata.FindNavigation(nameof(BlogCategory.Children));
        if (childrenNavigation is not null)
        {
            childrenNavigation.SetField("_children");
            childrenNavigation.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
