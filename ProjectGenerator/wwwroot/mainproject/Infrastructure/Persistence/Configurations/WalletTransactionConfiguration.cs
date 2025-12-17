using Attar.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(transaction => transaction.BalanceAfterTransaction)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(transaction => transaction.Reference)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(transaction => transaction.Description)
            .HasMaxLength(512);

        builder.Property(transaction => transaction.Metadata)
            .HasMaxLength(2000);

        builder.Property(transaction => transaction.Type)
            .HasConversion<int>();

        builder.Property(transaction => transaction.Status)
            .HasConversion<int>();

        builder.Property(transaction => transaction.OccurredAt)
            .IsRequired();

        builder.HasIndex(transaction => transaction.WalletAccountId);
        builder.HasIndex(transaction => transaction.InvoiceId);
        builder.HasIndex(transaction => transaction.PaymentTransactionId);
    }
}
