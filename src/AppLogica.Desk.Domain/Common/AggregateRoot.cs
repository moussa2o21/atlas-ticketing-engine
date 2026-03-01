namespace AppLogica.Desk.Domain.Common;

/// <summary>
/// Base class for aggregate roots. Extends <see cref="Entity"/> with domain event
/// support so that state transitions can raise events for eventual dispatch.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Domain events raised by this aggregate that have not yet been dispatched.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Enqueues a domain event to be dispatched after the aggregate is persisted.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all pending domain events. Called by the infrastructure layer
    /// after events have been dispatched.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
