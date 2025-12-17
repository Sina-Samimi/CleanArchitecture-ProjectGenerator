using TestAttarClone.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TestAttarClone.Infrastructure.Persistence.Configurations;

public sealed class ProductViolationReportConfiguration : IEntityTypeConfiguration<ProductViolationReport>
{
    public void Configure(EntityTypeBuilder<ProductViolationReport> builder)
    {
        builder.HasKey(report => report.Id);

        builder.Property(report => report.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(report => report.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(report => report.ReporterId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(report => report.ReporterPhone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(report => report.SellerId)
            .HasMaxLength(450);

        builder.Property(report => report.ReviewedById)
            .HasMaxLength(450);

        builder.Property(report => report.IsReviewed)
            .HasDefaultValue(false);

        builder.HasOne(report => report.Product)
            .WithMany()
            .HasForeignKey(report => report.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(report => report.ProductOffer)
            .WithMany()
            .HasForeignKey(report => report.ProductOfferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(report => report.ProductId);
        builder.HasIndex(report => report.ProductOfferId);
        builder.HasIndex(report => report.SellerId);
        builder.HasIndex(report => report.ReporterId);
        builder.HasIndex(report => new { report.ProductId, report.CreateDate });
        builder.HasIndex(report => new { report.SellerId, report.IsReviewed });
    }
}

