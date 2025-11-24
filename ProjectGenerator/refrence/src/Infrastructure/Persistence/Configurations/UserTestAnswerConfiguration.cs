using Arsis.Domain.Entities.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class UserTestAnswerConfiguration : IEntityTypeConfiguration<UserTestAnswer>
{
    public void Configure(EntityTypeBuilder<UserTestAnswer> builder)
    {
        builder.HasKey(answer => answer.Id);

        builder.Property(answer => answer.AttemptId)
            .IsRequired();

        builder.Property(answer => answer.QuestionId)
            .IsRequired();

        builder.Property(answer => answer.SelectedOptionId)
            .IsRequired(false);

        builder.Property(answer => answer.TextAnswer)
            .IsRequired(false);

        builder.Property(answer => answer.LikertValue)
            .IsRequired(false);

        builder.Property(answer => answer.IsCorrect)
            .IsRequired(false);

        builder.Property(answer => answer.Score)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(answer => answer.AnsweredAt)
            .IsRequired();

        builder.HasOne(answer => answer.Question)
            .WithMany()
            .HasForeignKey(answer => answer.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(answer => answer.SelectedOption)
            .WithMany()
            .HasForeignKey(answer => answer.SelectedOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(answer => answer.AttemptId);
        builder.HasIndex(answer => answer.QuestionId);
        builder.HasIndex(answer => new { answer.AttemptId, answer.QuestionId });
    }
}
