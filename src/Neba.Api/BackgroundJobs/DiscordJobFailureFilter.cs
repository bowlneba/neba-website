using System.Diagnostics.CodeAnalysis;

using Hangfire.Server;

using Neba.Api.Discord;

namespace Neba.Api.BackgroundJobs;

/// <summary>
/// Posts a Discord alert whenever a job finishes with an unhandled exception - a safety net for
/// scheduled/recurring jobs (e.g. create-next-season, sync-document-*) that have no natural
/// moment where a human would otherwise notice a failure.
/// </summary>
internal sealed class DiscordJobFailureFilter(IDiscordNotifier discordNotifier) : IServerFilter
{
    public void OnPerforming(PerformingContext context)
    {
        // No implementation needed
    }

    [SuppressMessage("Usage", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks",
        Justification = "IServerFilter.OnPerformed is a synchronous Hangfire callback with no async overload; runs on a Hangfire worker thread with no captured SynchronizationContext, and IDiscordNotifier.NotifyAsync is guaranteed never to throw and bounded by its own short HTTP timeouts, so this cannot deadlock or hang.")]
    public void OnPerformed(PerformedContext context)
    {
        if (context.Exception is null || context.ExceptionHandled)
        {
            return;
        }

        var alert = new DiscordAlert(
            DiscordAlertSeverity.Warning,
            "Recurring job failed",
            context.Exception.Message,
            new Dictionary<string, string>
            {
                ["JobName"] = context.BackgroundJob.Job.Method.Name
            });

        discordNotifier.NotifyAsync(alert, CancellationToken.None).GetAwaiter().GetResult();
    }
}
