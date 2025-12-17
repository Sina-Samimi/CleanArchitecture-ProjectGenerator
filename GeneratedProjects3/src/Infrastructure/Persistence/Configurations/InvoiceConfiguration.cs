using LogTableRenameTest.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogTableRenameTest.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(invoice => invoice.Id);

        builder.Property(invoice => invoice.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(invoice => invoice.InvoiceNumber)
            .IsUnique();

        builder.Property(invoice => invoice.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(invoice => invoice.Description)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(invoice => invoice.Currency)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(invoice => invoice.UserId)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.Property(invoice => invoice.IssueDate)
            .IsRequired();

        builder.Property(invoice => invoice.DueDate)
            .IsRequired(false);

        builder.Property(invoice => invoice.ExternalReference)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(invoice => invoice.ShippingAddressId)
            .IsRequired(false);

        builder.Property(invoice => invoice.ShippingRecipientName)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(invoice => invoice.ShippingRecipientPhone)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(invoice => invoice.ShippingProvince)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(invoice => invoice.ShippingCity)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(invoice => invoice.ShippingPostalCode)
            .HasMaxLength(10)
            .IsRequired(false);

        builder.Property(invoice => invoice.ShippingAddressLine)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(invoice => invoice.ShippingPlaque)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(invoice => invoice.ShippingUnit)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(invoice => invoice.Status)
            .HasConversion<int>();

        builder.Property(invoice => invoice.TaxAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(invoice => invoice.AdjustmentAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Ignore(invoice => invoice.Items);
        builder.Ignore(invoice => invoice.Transactions);

        builder.HasMany(invoice => invoice.ItemsCollection)
            .WithOne(item => item.Invoice)
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        if (builder.Metadata.FindNavigation(nameof(Invoice.ItemsCollection)) is { } itemsNavigation)
        {
            itemsNavigation.SetField("_items");
            itemsNavigation.SetPropertyAccessMode(PropertyAccessMode.Field);
        }

        builder.HasMany(invoice => invoice.TransactionsCollection)
            .WithOne(transaction => transaction.Invoice)
            .HasForeignKey(transaction => transaction.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        if (builder.Metadata.FindNavigation(nameof(Invoice.TransactionsCollection)) is { } transactionsNavigation)
        {
            transactionsNavigation.SetField("_transactions");
            transactionsNavigation.SetPropertyAccessMode(PropertyAccessMode.Field);
        }

        builder.HasIndex(invoice => invoice.UserId);
    }
}
