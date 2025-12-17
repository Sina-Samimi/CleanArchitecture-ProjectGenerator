using TestAttarClone.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TestAttarClone.Infrastructure.Persistence.Configurations;

public sealed class ProductFaqConfiguration : IEntityTypeConfiguration<ProductFaq>
{
    public void Configure(EntityTypeBuilder<ProductFaq> builder)
    {
        builder.HasKey(faq => faq.Id);

        builder.Property(faq => faq.Question)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(faq => faq.Answer)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(faq => faq.DisplayOrder)
            .HasDefaultValue(0);

        builder.HasIndex(faq => new { faq.ProductId, faq.DisplayOrder });
    }
}
