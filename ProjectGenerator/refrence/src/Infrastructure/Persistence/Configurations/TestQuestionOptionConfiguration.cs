using Arsis.Domain.Entities.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class TestQuestionOptionConfiguration : IEntityTypeConfiguration<TestQuestionOption>
{
    public void Configure(EntityTypeBuilder<TestQuestionOption> builder)
    {
        builder.ToTable("TestQuestionOptions");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.QuestionId)
            .IsRequired();

        builder.Property(o => o.Text)
            .IsRequired();

        builder.Property(o => o.IsCorrect)
            .IsRequired();

        builder.Property(o => o.Score)
            .IsRequired(false);

        builder.Property(o => o.ImageUrl)
            .HasMaxLength(600)
            .IsRequired(false);

        builder.Property(o => o.Explanation)
            .IsRequired(false);

        builder.Property(o => o.Order)
            .IsRequired();

        // Relationships
        builder.HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.QuestionId);
        builder.HasIndex(o => o.Order);
    }
}
