using Microsoft.EntityFrameworkCore.Diagnostics;

using Neba.Api.BackgroundJobs;
using Neba.Api.Domain;

namespace Neba.Api.Database.Interceptors;

internal sealed class DomainEventDispatcherInterceptor(IDomainEventPublisher domainEventPublisher)
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

        foreach (var aggregate in aggregates)
        {
            domainEventPublisher.PublishAndClear(aggregate);
        }
    }
}