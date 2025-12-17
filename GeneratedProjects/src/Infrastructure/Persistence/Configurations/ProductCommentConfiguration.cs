using TestAttarClone.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TestAttarClone.Infrastructure.Persistence.Configurations;

public sealed class ProductCommentConfiguration : IEntityTypeConfiguration<ProductComment>
{
    public void Configure(EntityTypeBuilder<ProductComment> builder)
    {
        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.AuthorName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(comment => comment.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(comment => comment.Rating)
            .HasColumnType("float")
            .HasDefaultValue(0d);

        builder.Property(comment => comment.IsApproved)
            .HasDefaultValue(false);

        builder.Property(comment => comment.ApprovedById)
            .HasMaxLength(450);

        builder.Property(comment => comment.ApprovedAt);

        builder.HasOne(comment => comment.Product)
            .WithMany(product => product.Comments)
            .HasForeignKey(comment => comment.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(comment => comment.Parent)
            .WithMany()
            .HasForeignKey(comment => comment.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(comment => comment.ApprovedBy)
            .WithMany()
            .HasForeignKey(comment => comment.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(comment => new { comment.ProductId, comment.CreateDate });
    }
}
