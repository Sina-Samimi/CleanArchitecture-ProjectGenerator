using Arsis.Domain.Entities.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class TestQuestionConfiguration : IEntityTypeConfiguration<TestQuestion>
{
    public void Configure(EntityTypeBuilder<TestQuestion> builder)
    {
        builder.ToTable("TestQuestions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.TestId)
            .IsRequired();

        builder.Property(q => q.Text)
            .IsRequired();

        builder.Property(q => q.QuestionType)
            .IsRequired();

        builder.Property(q => q.Order)
            .IsRequired();

        builder.Property(q => q.Score)
            .IsRequired(false);

        builder.Property(q => q.IsRequired)
            .HasDefaultValue(true);

        builder.Property(q => q.ImageUrl)
            .HasMaxLength(600)
            .IsRequired(false);

        builder.Property(q => q.Explanation)
            .IsRequired(false);

        // Relationships
        builder.HasOne(q => q.Test)
            .WithMany(t => t.Questions)
            .HasForeignKey(q => q.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => q.TestId);
        builder.HasIndex(q => q.Order);
    }
}
