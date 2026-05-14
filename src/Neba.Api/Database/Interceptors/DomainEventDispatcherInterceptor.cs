using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Microsoft.EntityFrameworkCore.Diagnostics;

using Neba.Api.BackgroundJobs;
using Neba.Domain;

namespace Neba.Api.Database.Interceptors;

internal sealed class DomainEventDispatcherInterceptor(IBackgroundJobClient backgroundJobClient)
    : SaveChangesInterceptor
{
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchDomainEvents(eventData);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        DispatchDomainEvents(eventData);
        return new ValueTask<int>(result);
    }

    private void DispatchDomainEvents(SaveChangesCompletedEventData eventData)
    {
        if (eventData.Context is null)
        {
            return;
        }

        var aggregates = eventData.Context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0)
        {
            return;
        }

        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var domainEvent in events)
        {
            var eventType = domainEvent.GetType();
            var jobType = typeof(IDomainEventJob<>).MakeGenericType(eventType);
            var method = jobType.GetMethod(nameof(IDomainEventJob<IDomainEvent>.HandleAsync))!;
            var job = new Job(jobType, method, domainEvent, CancellationToken.None);
            backgroundJobClient.Create(job, new EnqueuedState());
        }

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }
    }
}