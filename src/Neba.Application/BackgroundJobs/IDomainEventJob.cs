using Neba.Domain;

namespace Neba.Application.BackgroundJobs;

#pragma warning disable S3246 // TEvent is intentionally invariant — contravariance is unsupported by DI containers and Hangfire's closed-type resolution

/// <summary>
/// Represents a Hangfire background job handler for a domain event.
/// </summary>
/// <typeparam name="TEvent">The domain event type this job handles.</typeparam>
public interface IDomainEventJob<TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task HandleAsync(TEvent domainEvent, CancellationToken ct);
}
