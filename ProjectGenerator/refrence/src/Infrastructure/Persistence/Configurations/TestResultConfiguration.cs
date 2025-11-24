using Arsis.Domain.Entities.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class TestResultConfiguration : IEntityTypeConfiguration<TestResult>
{
    public void Configure(EntityTypeBuilder<TestResult> builder)
    {
        builder.HasKey(result => result.Id);

        builder.Property(result => result.AttemptId)
            .IsRequired();

        builder.Property(result => result.ResultType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(result => result.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(result => result.Description)
            .IsRequired();

        builder.Property(result => result.Score)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(result => result.Rank)
            .IsRequired(false);

        builder.Property(result => result.AdditionalData)
            .IsRequired(false);

        builder.HasOne(result => result.Attempt)
            .WithMany()
            .HasForeignKey(result => result.AttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(result => result.AttemptId);
        builder.HasIndex(result => result.ResultType);
    }
}
