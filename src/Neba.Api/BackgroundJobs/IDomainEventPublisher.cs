using Neba.Api.Domain;

namespace Neba.Api.BackgroundJobs;

/// <summary>
/// Enqueues the Hangfire job that handles a domain event. Aggregates reach it through the save interceptor;
/// jobs that change rows no aggregate owns publish directly. It is the only path that enqueues an event job.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>Enqueues the <see cref="IDomainEventJob{TEvent}"/> for the event's runtime type.</summary>
    void Publish(IDomainEvent domainEvent);

    /// <summary>Publishes every event the aggregate holds, then clears them. Safe to call twice.</summary>
    void PublishAndClear(IAggregateRoot aggregate);
}