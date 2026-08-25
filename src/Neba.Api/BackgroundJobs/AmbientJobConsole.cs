using Hangfire.Server;

namespace Neba.Api.BackgroundJobs;

/// <summary>
/// Ambient bridge from ILogger to Hangfire.Console's per-job dashboard output. AsyncLocal-backed,
/// same rationale as Neba.Api.Identity.AmbientActorContext: Hangfire has no HttpContext-like
/// mechanism to hand a job's PerformContext to arbitrary code further down its own call stack, so
/// HangfireConsoleServerFilter stashes it here for the duration of the job, and
/// HangfireConsoleLoggerProvider reads it on every ILogger call made anywhere in that job's async
/// call chain (including inside [LoggerMessage] source-generated methods).
/// </summary>
internal static class AmbientJobConsole
{
    private static readonly AsyncLocal<PerformContext?> Current = new();

    public static PerformContext? Context => Current.Value;

    internal static void Set(PerformContext context) => Current.Value = context;

    internal static void Clear() => Current.Value = null;
}
