namespace Neba.Domain;

/// <summary>
/// Base class for aggregate roots that can raise domain events
/// </summary>
public abstract class AggregateRoot
    : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents
        => _domainEvents;

    /// <inheritdoc />
    public void ClearDomainEvents()
        => _domainEvents.Clear();

    /// <summary>
    /// Adds a domain event to the aggregate root
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);
}

/// <summary>
/// Interface for aggregate roots that can raise domain events
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Collection of domain events raised by the aggregate root
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events from the aggregate root
    /// </summary>
    void ClearDomainEvents();
}