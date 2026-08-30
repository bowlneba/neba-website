using System.Diagnostics.CodeAnalysis;

using Hangfire;
using Hangfire.States;

using Neba.Api.Discord;

namespace Neba.Api.BackgroundJobs;

/// <summary>
/// Posts a Discord alert when a job's state election settles on <see cref="FailedState"/> - a
/// safety net for scheduled/recurring jobs (e.g. create-next-season, sync-document-*) that have
/// no natural moment where a human would otherwise notice a failure.
/// </summary>
/// <remarks>
/// Implemented as <see cref="IElectStateFilter"/>, not <see cref="Hangfire.Server.IServerFilter"/>.
/// <see cref="AutomaticRetryAttribute"/> decides retry-vs-final-failure during state election, not
/// during job performance, so an <c>IServerFilter.OnPerformed</c> callback sees every failed
/// attempt - including ones Hangfire is about to silently retry - with no way to tell them apart.
/// Registering this filter after <see cref="AutomaticRetryAttribute"/> in
/// <c>BackgroundJobsConfiguration.AddHangfireInfrastructure</c> (both default to the same filter
/// Order, so registration order is preserved) means this filter's <see cref="OnStateElection"/>
/// only runs once <see cref="AutomaticRetryAttribute"/> has already rewritten
/// <see cref="ElectStateContext.CandidateState"/> to a retry <see cref="ScheduledState"/> when
/// attempts remain - so a job still in <see cref="FailedState"/> here has genuinely exhausted its
/// retries.
/// </remarks>
internal sealed class DiscordJobFailureFilter(IDiscordNotifier discordNotifier) : IElectStateFilter
{
    [SuppressMessage("Usage", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks",
        Justification = "IElectStateFilter.OnStateElection is a synchronous Hangfire callback with no async overload; runs on a Hangfire worker thread with no captured SynchronizationContext, and IDiscordNotifier.NotifyAsync is guaranteed never to throw and bounded by its own short HTTP timeouts, so this cannot deadlock or hang.")]
    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not FailedState failedState)
        {
            return;
        }

        var alert = new DiscordAlert(
            DiscordAlertSeverity.Warning,
            "Recurring job failed",
            failedState.Exception.Message,
            new Dictionary<string, string>
            {
                ["JobName"] = context.BackgroundJob.Job.Method.Name
            });

        discordNotifier.NotifyAsync(alert, CancellationToken.None).GetAwaiter().GetResult();
    }
}
