using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Attar.Domain.Base;

public abstract class Entity : BaseEntity<Guid>
{
    private readonly List<object> _domainEvents = new();

    [SetsRequiredMembers]
    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(object domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
