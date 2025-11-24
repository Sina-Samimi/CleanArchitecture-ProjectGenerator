using Arsis.Domain.Entities.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class UserTestAttemptConfiguration : IEntityTypeConfiguration<UserTestAttempt>
{
    public void Configure(EntityTypeBuilder<UserTestAttempt> builder)
    {
        builder.HasKey(attempt => attempt.Id);

        builder.Property(attempt => attempt.TestId)
            .IsRequired();

        builder.Property(attempt => attempt.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(attempt => attempt.AttemptNumber)
            .IsRequired();

        builder.Property(attempt => attempt.Status)
            .IsRequired();

        builder.Property(attempt => attempt.StartedAt)
            .IsRequired();

        builder.Property(attempt => attempt.CompletedAt)
            .IsRequired(false);

        builder.Property(attempt => attempt.ExpiresAt)
            .IsRequired(false);

        builder.Property(attempt => attempt.TotalScore)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(attempt => attempt.MaxScore)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(attempt => attempt.ScorePercentage)
            .HasColumnType("decimal(5,2)")
            .IsRequired(false);

        builder.Property(attempt => attempt.IsPassed)
            .IsRequired(false);

        builder.Property(attempt => attempt.InvoiceId)
            .IsRequired(false);

        builder.HasMany(attempt => attempt.Answers)
            .WithOne(answer => answer.Attempt)
            .HasForeignKey(answer => answer.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(attempt => attempt.Answers)
            .HasField("_answers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(attempt => attempt.User)
            .WithMany()
            .HasForeignKey(attempt => attempt.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(attempt => attempt.TestId);
        builder.HasIndex(attempt => attempt.UserId);
        builder.HasIndex(attempt => attempt.Status);
        builder.HasIndex(attempt => new { attempt.TestId, attempt.UserId, attempt.AttemptNumber });
    }
}
