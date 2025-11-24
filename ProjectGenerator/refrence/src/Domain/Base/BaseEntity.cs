using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Arsis.Domain.Constants;
using Arsis.Domain.Entities;

namespace Arsis.Domain.Base;

public abstract class BaseEntity
{
    [SetsRequiredMembers]
    protected BaseEntity()
    {
        Ip = IPAddress.None;
        CreatorId = SystemUsers.AutomationId;
        CreateDate = DateTimeOffset.UtcNow;
        UpdateDate = DateTimeOffset.UtcNow;
    }

    public required IPAddress Ip { get; set; }

    public required string CreatorId { get; set; }

    public string? UpdaterId { get; set; }

    public DateTimeOffset CreateDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset UpdateDate { get; set; }

    public DateTimeOffset? RemoveDate { get; set; }

    #region Relations
    [ForeignKey(nameof(CreatorId))]
    public ApplicationUser Creator { get; set; } = null!;

    [ForeignKey(nameof(UpdaterId))]
    public ApplicationUser? Updater { get; set; }
    #endregion
}

public abstract class BaseEntity<T> : BaseEntity
{
    [SetsRequiredMembers]
    protected BaseEntity()
    {
    }

    public virtual T Id { get; protected set; } = default!;
}
