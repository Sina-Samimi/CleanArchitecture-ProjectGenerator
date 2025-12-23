using MobiRooz.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MobiRooz.Infrastructure.Persistence.Configurations;

public sealed class DeploymentProfileConfiguration : IEntityTypeConfiguration<DeploymentProfile>
{
    public void Configure(EntityTypeBuilder<DeploymentProfile> builder)
    {
        builder.ToTable("DeploymentProfiles");

        builder.Property(profile => profile.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(profile => profile.Branch)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(profile => profile.ServerHost)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(profile => profile.ServerUser)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(profile => profile.DestinationPath)
            .IsRequired()
            .HasMaxLength(400);

        builder.Property(profile => profile.ArtifactName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(profile => profile.PreDeployCommand)
            .HasMaxLength(1000);

        builder.Property(profile => profile.PostDeployCommand)
            .HasMaxLength(1000);

        builder.Property(profile => profile.ServiceReloadCommand)
            .HasMaxLength(400);

        builder.Property(profile => profile.SecretKeyName)
            .HasMaxLength(200);

        builder.Property(profile => profile.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(profile => profile.Name).IsUnique();
        builder.HasIndex(profile => profile.Branch).IsUnique();
    }
}
