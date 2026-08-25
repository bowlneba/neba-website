using Hangfire.Server;

namespace Neba.Api.BackgroundJobs;

/// <summary>
/// Makes every job's PerformContext available to HangfireConsoleLoggerProvider via
/// AmbientJobConsole for the duration of that job's execution.
/// </summary>
/// <remarks>
/// Deliberately AsyncLocal-backed rather than PerformContext.Items-backed. This codebase has a
/// documented bug class where two Hangfire filters (Audit.Hangfire's AuditJobExecutionFilterAttribute
/// plus a duplicate) both stashed state in PerformContext.Items under the same fixed string key
/// and clobbered each other. Using AmbientJobConsole's own AsyncLocal instead of Items sidesteps
/// that class of collision entirely - this filter shares no storage with Audit.Hangfire's.
/// </remarks>
internal sealed class HangfireConsoleServerFilter : IServerFilter
{
    public void OnPerforming(PerformingContext context) => AmbientJobConsole.Set(context);

    public void OnPerformed(PerformedContext context) => AmbientJobConsole.Clear();
}
