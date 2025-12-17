using LogsDtoCloneTest.Domain.Entities.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Configurations;

public sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("Pages");

        builder.HasKey(page => page.Id);

        builder.Property(page => page.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(page => page.Slug)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(page => page.Slug)
            .IsUnique();

        builder.Property(page => page.Content)
            .IsRequired();

        builder.Property(page => page.MetaTitle)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(page => page.MetaDescription)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(page => page.MetaKeywords)
            .HasMaxLength(400)
            .IsRequired(false);

        builder.Property(page => page.MetaRobots)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(page => page.IsPublished)
            .IsRequired();

        builder.Property(page => page.ViewCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(page => page.FeaturedImagePath)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(page => page.ShowInFooter)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(page => page.ShowInQuickAccess)
            .IsRequired()
            .HasDefaultValue(false);
    }
}

