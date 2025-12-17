using LogTableRenameTest.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogTableRenameTest.Infrastructure.Persistence.Configurations;

public sealed class ProductCustomRequestConfiguration : IEntityTypeConfiguration<ProductCustomRequest>
{
    public void Configure(EntityTypeBuilder<ProductCustomRequest> builder)
    {
        builder.ToTable("ProductCustomRequests");

        builder.HasKey(request => request.Id);

        builder.HasOne(request => request.Product)
            .WithMany()
            .HasForeignKey(request => request.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(request => request.ProductId)
            .IsRequired();

        builder.Property(request => request.UserId)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.Property(request => request.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(request => request.Phone)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(request => request.Email)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(request => request.Message)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(request => request.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(request => request.ContactedAt)
            .IsRequired(false);

        builder.Property(request => request.AdminNotes)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.HasIndex(request => request.ProductId);
        builder.HasIndex(request => request.UserId);
        builder.HasIndex(request => request.Status);
        builder.HasIndex(request => request.CreateDate);
    }
}

