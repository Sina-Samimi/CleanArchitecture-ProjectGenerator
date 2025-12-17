using LogTableRenameTest.Domain.Entities.Seo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogTableRenameTest.Infrastructure.Persistence.Configurations;

public sealed class SeoMetadataConfiguration : IEntityTypeConfiguration<SeoMetadata>
{
    public void Configure(EntityTypeBuilder<SeoMetadata> builder)
    {
        builder.ToTable("SeoMetadata");

        builder.HasKey(seo => seo.Id);

        builder.Property(seo => seo.PageType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(seo => seo.PageIdentifier)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(seo => seo.MetaTitle)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(seo => seo.MetaDescription)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(seo => seo.MetaKeywords)
            .HasMaxLength(400)
            .IsRequired(false);

        builder.Property(seo => seo.MetaRobots)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(seo => seo.CanonicalUrl)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(seo => seo.UseTemplate)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(seo => seo.TitleTemplate)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(seo => seo.DescriptionTemplate)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(seo => seo.OgTitleTemplate)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(seo => seo.OgDescriptionTemplate)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(seo => seo.RobotsTemplate)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(seo => seo.OgTitle)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(seo => seo.OgDescription)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(seo => seo.OgImage)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(seo => seo.OgType)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(seo => seo.OgUrl)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(seo => seo.TwitterCard)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(seo => seo.TwitterTitle)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(seo => seo.TwitterDescription)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(seo => seo.TwitterImage)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(seo => seo.SchemaJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(seo => seo.BreadcrumbsJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(seo => seo.SitemapPriority)
            .HasColumnType("decimal(3,2)")
            .IsRequired(false);

        builder.Property(seo => seo.SitemapChangefreq)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(seo => seo.H1Title)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(seo => seo.FeaturedImageUrl)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(seo => seo.FeaturedImageAlt)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(seo => seo.Tags)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(seo => seo.Description)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.HasIndex(seo => new { seo.PageType, seo.PageIdentifier })
            .IsUnique()
            .HasFilter("[PageIdentifier] IS NOT NULL");

        builder.HasIndex(seo => seo.PageType)
            .HasFilter("[PageIdentifier] IS NULL");
    }
}

