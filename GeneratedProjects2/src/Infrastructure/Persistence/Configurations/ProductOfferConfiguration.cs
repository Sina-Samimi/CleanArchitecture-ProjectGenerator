using LogsDtoCloneTest.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Configurations;

public sealed class ProductOfferConfiguration : IEntityTypeConfiguration<ProductOffer>
{
    public void Configure(EntityTypeBuilder<ProductOffer> builder)
    {
        builder.ToTable("ProductOffers");

        builder.HasKey(offer => offer.Id);

        builder.Property(offer => offer.SellerId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(offer => offer.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(offer => offer.CompareAtPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(offer => offer.StockQuantity)
            .HasDefaultValue(0);

        builder.Property(offer => offer.IsActive)
            .HasDefaultValue(true);

        builder.Property(offer => offer.IsPublished)
            .HasDefaultValue(false);

        builder.Property(offer => offer.PublishedAt)
            .IsRequired(false);

        builder.Property(offer => offer.ApprovedFromRequestId)
            .IsRequired(false);

        builder.HasOne(offer => offer.Product)
            .WithMany()
            .HasForeignKey(offer => offer.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(offer => offer.ApprovedFromRequest)
            .WithMany()
            .HasForeignKey(offer => offer.ApprovedFromRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique constraint: One offer per seller per product
        builder.HasIndex(offer => new { offer.ProductId, offer.SellerId })
            .IsUnique();

        builder.HasIndex(offer => offer.ProductId);
        builder.HasIndex(offer => offer.SellerId);
        builder.HasIndex(offer => offer.IsActive);
        builder.HasIndex(offer => offer.IsPublished);
        builder.HasIndex(offer => offer.CreateDate);
    }
}
