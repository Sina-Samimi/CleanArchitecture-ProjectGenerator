using Microsoft.AspNetCore.Identity;

namespace MobiRooz.Domain.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? DeactivatedOn { get; set; }

    public string? DeactivationReason { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedOn { get; set; }

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastModifiedOn { get; set; } = DateTimeOffset.UtcNow;

    public string? AvatarPath { get; set; }
}
