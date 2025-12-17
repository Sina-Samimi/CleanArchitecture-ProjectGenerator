using Attar.Domain.Base;
using Attar.Domain.Entities;
using Attar.Domain.Entities.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attar.Infrastructure.Persistence.Configurations;

public sealed class NavigationMenuItemConfiguration : IEntityTypeConfiguration<NavigationMenuItem>
{
    public void Configure(EntityTypeBuilder<NavigationMenuItem> builder)
    {
        builder.ToTable("NavigationMenuItems");

        builder.Property(item => item.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(item => item.Url)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(item => item.Icon)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(item => item.ImageUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(item => item.DisplayOrder)
            .HasDefaultValue(0);

        builder.HasIndex(item => item.ParentId);
        builder.HasIndex(item => new { item.ParentId, item.DisplayOrder });

        builder.HasOne(item => item.Parent)
            .WithMany(parent => parent.Children)
            .HasForeignKey(item => item.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.Creator)
            .WithMany()
            .HasForeignKey(item => item.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.Updater)
            .WithMany()
            .HasForeignKey(item => item.UpdaterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
