using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Neba.Api.Domain;

namespace Neba.Api.BackgroundJobs;

internal sealed class DomainEventPublisher(IBackgroundJobClient backgroundJobClient)
    : IDomainEventPublisher
{
    public void Publish(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var jobType = typeof(IDomainEventJob<>).MakeGenericType(domainEvent.GetType());
        var method = jobType.GetMethod(nameof(IDomainEventJob<>.HandleAsync))!;

        backgroundJobClient.Create(new Job(jobType, method, domainEvent, CancellationToken.None), new EnqueuedState());
    }

    public void PublishAndClear(IAggregateRoot aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        foreach (var domainEvent in aggregate.DomainEvents.ToList())
        {
            Publish(domainEvent);
        }

        aggregate.ClearDomainEvents();
    }
}