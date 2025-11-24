using Arsis.Domain.Entities.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class TestConfiguration : IEntityTypeConfiguration<Test>
{
    public void Configure(EntityTypeBuilder<Test> builder)
    {
        builder.ToTable("Tests");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(t => t.Description)
            .IsRequired();

        builder.Property(t => t.Type)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired();

        builder.Property(t => t.Price)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(t => t.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("IRT");

        builder.Property(t => t.DurationMinutes)
            .IsRequired(false);

        builder.Property(t => t.MaxAttempts)
            .IsRequired(false);

        builder.Property(t => t.ShowResultsImmediately)
            .HasDefaultValue(true);

        builder.Property(t => t.ShowCorrectAnswers)
            .HasDefaultValue(false);

        builder.Property(t => t.RandomizeQuestions)
            .HasDefaultValue(false);

        builder.Property(t => t.RandomizeOptions)
            .HasDefaultValue(false);

        builder.Property(t => t.AvailableFrom)
            .IsRequired(false);

        builder.Property(t => t.AvailableUntil)
            .IsRequired(false);

        builder.Property(t => t.NumberOfQuestionsToShow)
            .IsRequired(false);

        builder.Property(t => t.PassingScore)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        // Relationships
        builder.HasMany(t => t.Questions)
            .WithOne(q => q.Test)
            .HasForeignKey(q => q.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Attempts)
            .WithOne(a => a.Test)
            .HasForeignKey(a => a.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(t => t.CategoryId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CreateDate);
    }
}
