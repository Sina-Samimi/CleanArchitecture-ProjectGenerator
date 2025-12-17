using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.Application.Interfaces;
using LogsDtoCloneTest.Domain.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LogsDtoCloneTest.Infrastructure.Persistence.Interceptors;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IAuditContext _auditContext;

    public AuditInterceptor(IAuditContext auditContext)
    {
        _auditContext = auditContext;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context == null)
            return;

        var audit = _auditContext.Capture();

        var entries = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(BaseEntity.CreatorId)).CurrentValue = audit.UserId;
                entry.Property(nameof(BaseEntity.CreateDate)).CurrentValue = audit.Timestamp;
                entry.Property(nameof(BaseEntity.Ip)).CurrentValue = audit.IpAddress;
            }
            
            // Always update these fields for both Added and Modified
            // Use Property().CurrentValue to avoid triggering change detection conflicts
            entry.Property(nameof(BaseEntity.UpdateDate)).CurrentValue = audit.Timestamp;
            entry.Property(nameof(BaseEntity.UpdaterId)).CurrentValue = audit.UserId;
            entry.Property(nameof(BaseEntity.Ip)).CurrentValue = audit.IpAddress;
        }
    }
}
