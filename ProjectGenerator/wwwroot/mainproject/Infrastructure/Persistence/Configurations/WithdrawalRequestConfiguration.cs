using Attar.Domain.Entities;
using Attar.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class WithdrawalRequestConfiguration : IEntityTypeConfiguration<WithdrawalRequest>
{
    public void Configure(EntityTypeBuilder<WithdrawalRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.SellerId)
            .HasMaxLength(450);

        builder.HasIndex(r => r.SellerId);
        
        builder.Property(r => r.UserId)
            .HasMaxLength(450);

        builder.HasIndex(r => r.UserId);
        
        builder.Property(r => r.RequestType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(r => r.Currency)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(r => r.BankAccountNumber)
            .HasMaxLength(100);
        
        builder.Property(r => r.CardNumber)
            .HasMaxLength(20);
        
        builder.Property(r => r.Iban)
            .HasMaxLength(34);

        builder.Property(r => r.BankName)
            .HasMaxLength(200);

        builder.Property(r => r.AccountHolderName)
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.AdminNotes)
            .HasMaxLength(2000);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.ProcessedByUserId)
            .HasMaxLength(450);

        builder.HasOne(r => r.ProcessedByUser)
            .WithMany()
            .HasForeignKey(r => r.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.ProcessedAt);

        builder.HasOne(r => r.WalletTransaction)
            .WithMany()
            .HasForeignKey(r => r.WalletTransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

