using Arsis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class TalentConfiguration : IEntityTypeConfiguration<Talent>
{
    public void Configure(EntityTypeBuilder<Talent> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(1000);

        builder.HasMany<TalentScore>()
            .WithOne()
            .HasForeignKey(score => score.TalentId);

        var scoresNavigation = builder.Metadata.FindNavigation(nameof(Talent.Scores));
        if (scoresNavigation is not null)
        {
            scoresNavigation.SetField("_scores");
            scoresNavigation.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
