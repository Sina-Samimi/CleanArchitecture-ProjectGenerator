using System.Collections.Generic;
using System.Linq;
using Arsis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arsis.Infrastructure.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Text).IsRequired().HasMaxLength(1000);
        var talentIdsComparer = new ValueComparer<List<Guid>>(
            (left, right) => left.SequenceEqual(right),
            ids => ids.Aggregate(0, (hash, id) => HashCode.Combine(hash, id.GetHashCode())),
            ids => ids.ToList());

        builder.Property<List<Guid>>("_talentIds")
            .HasColumnName("TalentIds")
            .HasConversion(
                v => string.Join(',', v),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<Guid>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList())
            .Metadata.SetValueComparer(talentIdsComparer);
    }
}
