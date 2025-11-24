using Arsis.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class WalletAccountConfiguration : IEntityTypeConfiguration<WalletAccount>
{
    public void Configure(EntityTypeBuilder<WalletAccount> builder)
    {
        builder.HasKey(account => account.Id);

        builder.Property(account => account.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(account => account.UserId)
            .IsUnique();

        builder.Property(account => account.Currency)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(account => account.Balance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(account => account.LastActivityOn)
            .IsRequired();

        builder.Property(account => account.IsLocked)
            .IsRequired();

        builder.Ignore(account => account.Transactions);

        builder.HasMany(account => account.TransactionsCollection)
            .WithOne(transaction => transaction.WalletAccount)
            .HasForeignKey(transaction => transaction.WalletAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        if (builder.Metadata.FindNavigation(nameof(WalletAccount.TransactionsCollection)) is { } navigation)
        {
            navigation.SetField("_transactions");
            navigation.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
