using Arsis.Domain.Entities;
using Arsis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class TalentScoreConfiguration : IEntityTypeConfiguration<TalentScore>
{
    public void Configure(EntityTypeBuilder<TalentScore> builder)
    {
        builder.HasKey(score => score.Id);
        builder.OwnsOne(score => score.Score, ownedBuilder =>
        {
            ownedBuilder.Property(s => s.Value).HasColumnName("Score").HasPrecision(5, 2);
        });

        builder.Property(score => score.CalculatedAt).IsRequired();
        builder.HasIndex(score => new { score.UserId, score.TalentId }).IsUnique();

        builder.HasOne<Talent>()
            .WithMany(t => t.Scores)
            .HasForeignKey(score => score.TalentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
