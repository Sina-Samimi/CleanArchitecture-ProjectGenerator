using LogsDtoCloneTest.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(transaction => transaction.Method)
            .HasConversion<int>();

        builder.Property(transaction => transaction.Status)
            .HasConversion<int>();

        builder.Property(transaction => transaction.Reference)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(transaction => transaction.GatewayName)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(transaction => transaction.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(transaction => transaction.Metadata)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(transaction => transaction.OccurredAt)
            .IsRequired();
    }
}
