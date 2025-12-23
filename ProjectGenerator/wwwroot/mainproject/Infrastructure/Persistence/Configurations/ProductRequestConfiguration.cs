using MobiRooz.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MobiRooz.Infrastructure.Persistence.Configurations;

public sealed class ProductRequestConfiguration : IEntityTypeConfiguration<ProductRequest>
{
    public void Configure(EntityTypeBuilder<ProductRequest> builder)
    {
        builder.ToTable("ProductRequests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(request => request.Summary)
            .HasMaxLength(600)
            .IsRequired(false);

        builder.Property(request => request.Description)
            .IsRequired();

        builder.Property(request => request.SeoTitle)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(request => request.SeoDescription)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(request => request.SeoKeywords)
            .HasMaxLength(400)
            .IsRequired(false);

        builder.Property(request => request.SeoSlug)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(request => request.Robots)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(request => request.FeaturedImagePath)
            .HasMaxLength(600)
            .IsRequired(false);

        builder.Property(request => request.TagList)
            .HasMaxLength(1000)
            .HasColumnName("Tags")
            .IsRequired(false);

        builder.Property(request => request.DigitalDownloadPath)
            .HasMaxLength(600)
            .IsRequired(false);

        builder.Property(request => request.SellerId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(request => request.Brand)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(request => request.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(request => request.StockQuantity)
            .HasDefaultValue(0);

        builder.Property(request => request.IsCustomOrder)
            .HasDefaultValue(false);

        builder.Property(request => request.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(request => request.ReviewedAt)
            .IsRequired(false);

        builder.Property(request => request.ReviewerId)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.Property(request => request.RejectionReason)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(request => request.ApprovedProductId)
            .IsRequired(false);

        builder.Property(request => request.TargetProductId)
            .IsRequired(false);

        builder.HasOne(request => request.Category)
            .WithMany()
            .HasForeignKey(request => request.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.ApprovedProduct)
            .WithMany()
            .HasForeignKey(request => request.ApprovedProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(request => request.TargetProduct)
            .WithMany()
            .HasForeignKey(request => request.TargetProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(request => request.Gallery)
            .WithOne(image => image.ProductRequest)
            .HasForeignKey(image => image.ProductRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(request => request.Gallery)
            .HasField("_gallery")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(request => request.SellerId);
        builder.HasIndex(request => request.Status);
        builder.HasIndex(request => request.CreateDate);
        builder.HasIndex(request => request.CategoryId);
        builder.HasIndex(request => request.ApprovedProductId);
        builder.HasIndex(request => request.TargetProductId);
        builder.HasIndex(request => request.SeoSlug);
    }
}

