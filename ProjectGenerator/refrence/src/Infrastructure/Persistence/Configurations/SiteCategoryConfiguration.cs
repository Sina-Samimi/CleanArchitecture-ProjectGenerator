using Arsis.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class SiteCategoryConfiguration : IEntityTypeConfiguration<SiteCategory>
{
    public void Configure(EntityTypeBuilder<SiteCategory> builder)
    {
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(category => category.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(category => category.Description)
            .HasMaxLength(600);

        builder.HasIndex(category => new { category.Scope, category.Slug })
            .IsUnique();

        builder.Property(category => category.Scope)
            .HasConversion<int>();

        builder.HasOne(category => category.Parent)
            .WithMany(parent => parent.Children)
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(category => category.Children)
            .HasField("_children")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
