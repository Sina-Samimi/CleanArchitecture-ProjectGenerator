using TestAttarClone.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TestAttarClone.Infrastructure.Persistence.Configurations;

public sealed class ProductExecutionStepConfiguration : IEntityTypeConfiguration<ProductExecutionStep>
{
    public void Configure(EntityTypeBuilder<ProductExecutionStep> builder)
    {
        builder.HasKey(step => step.Id);

        builder.Property(step => step.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(step => step.Description)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(step => step.Duration)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(step => step.DisplayOrder)
            .HasDefaultValue(0);

        builder.HasIndex(step => new { step.ProductId, step.DisplayOrder });
    }
}
