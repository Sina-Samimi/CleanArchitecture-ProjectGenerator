using Attar.Domain.Entities.Seo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class PageFaqConfiguration : IEntityTypeConfiguration<PageFaq>
{
    public void Configure(EntityTypeBuilder<PageFaq> builder)
    {
        builder.ToTable("PageFaqs");

        builder.HasKey(faq => faq.Id);

        builder.Property(faq => faq.PageType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(faq => faq.PageIdentifier)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(faq => faq.Question)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(faq => faq.Answer)
            .IsRequired()
            .HasMaxLength(3000);

        builder.Property(faq => faq.DisplayOrder)
            .HasDefaultValue(0);

        builder.HasIndex(faq => new { faq.PageType, faq.PageIdentifier, faq.DisplayOrder });
    }
}

