using LogTableRenameTest.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogTableRenameTest.Infrastructure.Persistence.Configurations;

public sealed class InvoiceItemAttributeConfiguration : IEntityTypeConfiguration<InvoiceItemAttribute>
{
    public void Configure(EntityTypeBuilder<InvoiceItemAttribute> builder)
    {
        builder.HasKey(attribute => attribute.Id);

        builder.Property(attribute => attribute.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(attribute => attribute.Value)
            .HasMaxLength(1000)
            .IsRequired();
    }
}
