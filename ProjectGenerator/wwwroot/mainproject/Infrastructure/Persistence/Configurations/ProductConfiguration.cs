using MobiRooz.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MobiRooz.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(product => product.Summary)
            .HasMaxLength(600)
            .IsRequired(false);

        builder.Property(product => product.Description)
            .IsRequired();

        builder.Property(product => product.SeoTitle)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(product => product.SeoDescription)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(product => product.SeoKeywords)
            .HasMaxLength(400)
            .IsRequired(false);

        builder.Property(product => product.SeoSlug)
            .HasMaxLength(250)
            .IsRequired(false);

        builder.HasIndex(product => product.SeoSlug)
            .IsUnique();

        builder.Property(product => product.Robots)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(product => product.FeaturedImagePath)
            .HasMaxLength(600)
            .IsRequired(false);

        builder.Property(product => product.TagList)
            .HasMaxLength(1000)
            .HasColumnName("Tags")
            .IsRequired(false);

        builder.Property(product => product.DigitalDownloadPath)
            .HasMaxLength(600)
            .IsRequired(false);

        builder.Property(product => product.SellerId)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.HasIndex(product => product.SellerId);

        builder.Property(product => product.Brand)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(product => product.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(product => product.CompareAtPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(product => product.IsCustomOrder)
            .HasDefaultValue(false);

        builder.Property(product => product.StockQuantity)
            .HasDefaultValue(0);

        builder.HasOne(product => product.Category)
            .WithMany()
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(product => product.Gallery)
            .WithOne(image => image.Product)
            .HasForeignKey(image => image.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(product => product.Gallery)
            .HasField("_gallery")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(product => product.ExecutionSteps)
            .WithOne(step => step.Product)
            .HasForeignKey(step => step.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(product => product.ExecutionSteps)
            .HasField("_executionSteps")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(product => product.Faqs)
            .WithOne(faq => faq.Product)
            .HasForeignKey(faq => faq.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(product => product.Faqs)
            .HasField("_faqs")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(product => product.Comments)
            .HasField("_comments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(product => product.Attributes)
            .WithOne(attribute => attribute.Product)
            .HasForeignKey(attribute => attribute.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(product => product.Attributes)
            .HasField("_attributes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(product => product.VariantAttributes)
            .WithOne(attr => attr.Product)
            .HasForeignKey(attr => attr.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(product => product.VariantAttributes)
            .HasField("_variantAttributes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(product => product.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(product => product.Variants)
            .HasField("_variants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(product => product.Tags);
        builder.Ignore(product => product.HasVariants);
    }
}
