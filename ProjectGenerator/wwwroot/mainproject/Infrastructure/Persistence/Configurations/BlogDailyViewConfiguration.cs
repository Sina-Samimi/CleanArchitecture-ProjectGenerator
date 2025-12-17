using System.Net;
using Attar.Domain.Entities.Blogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class BlogDailyViewConfiguration : IEntityTypeConfiguration<BlogDailyView>
{
    public void Configure(EntityTypeBuilder<BlogDailyView> builder)
    {
        builder.HasKey(view => view.Id);

        var ipConverter = new ValueConverter<IPAddress, string>(
            address => address.ToString(),
            value => string.IsNullOrWhiteSpace(value) ? IPAddress.None : IPAddress.Parse(value));

        builder.Property(view => view.ViewerIp)
            .HasConversion(ipConverter)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(view => view.ViewDate)
            .HasColumnType("date");

        builder.HasIndex(view => view.BlogId);

        builder.HasIndex(view => new { view.BlogId, view.ViewDate, view.ViewerIp })
            .IsUnique();
    }
}
