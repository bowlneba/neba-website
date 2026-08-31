using System.Reflection;

using Hangfire;
using Hangfire.States;

using Neba.Api.Compliance;
using Neba.Api.Discord;

namespace Neba.Api.BackgroundJobs;

/// <summary>
/// Marks a background job method (or its declaring type) as already posting its own Discord alert
/// on failure, so <see cref="DiscordJobFailureFilter"/> skips it rather than posting a second,
/// uncorrelated alert for the same failure. See <see cref="Legacy.PongJob.PongAsync"/> for the
/// motivating case: it posts an alert with richer context (the failing health-check response) on
/// every failed attempt, not just the final exhausted-retries one this filter reacts to.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
internal sealed class SkipDiscordJobFailureAlertAttribute : Attribute;

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
    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not FailedState failedState)
        {
            return;
        }

        var method = context.BackgroundJob.Job.Method;
        if (method.GetCustomAttribute<SkipDiscordJobFailureAlertAttribute>() is not null
            || method.DeclaringType?.GetCustomAttribute<SkipDiscordJobFailureAlertAttribute>() is not null)
        {
            return;
        }

        // DiscordMessageRedactor masks any embedded email address in the exception message, same
        // reasoning as GlobalExceptionHandler/ResilientAuditDataProvider's identical comment.
        var alert = new DiscordAlert(
            DiscordAlertSeverity.Warning,
            "Recurring job failed",
            DiscordMessageRedactor.Redact(failedState.Exception.Message),
            new Dictionary<string, string>
            {
                ["JobName"] = context.BackgroundJob.Job.Method.Name
            });

        // Fire-and-forget rather than blocking this Hangfire worker thread on the Discord HTTP
        // call: OnStateElection has no async overload, and with a low worker count (dev runs 1)
        // blocking here for the duration of DiscordNotifier's own timeout/retry policy would stall
        // every other job in the process. NotifyAsync already swallows every non-cancellation
        // failure internally, so there's nothing here to observe or retry.
        _ = Task.Run(() => discordNotifier.NotifyAsync(alert, CancellationToken.None));
    }
}