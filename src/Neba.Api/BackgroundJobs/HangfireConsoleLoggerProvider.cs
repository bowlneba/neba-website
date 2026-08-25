using Hangfire.Console;

namespace Neba.Api.BackgroundJobs;

/// <summary>
/// Bridges every ILogger call made during a Hangfire job's execution to that job's own
/// Hangfire.Console dashboard tab, via the PerformContext HangfireConsoleServerFilter stashes in
/// AmbientJobConsole. Registered alongside the app's other logging providers, so this applies
/// automatically to every existing and future job's [LoggerMessage] calls - no job needs to take a
/// PerformContext parameter or call WriteLine itself. Outside a job (e.g. a normal HTTP request),
/// AmbientJobConsole.Context is null and this provider is a no-op, so it never duplicates output
/// for request-scoped logging.
/// </summary>
internal sealed class HangfireConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new HangfireConsoleLogger(categoryName);

    public void Dispose()
    {
    }

    private sealed class HangfireConsoleLogger(string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => AmbientJobConsole.Context is not null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var context = AmbientJobConsole.Context;
            if (context is null)
            {
                return;
            }

            context.WriteLine(TextColorFor(logLevel), categoryName + ": " + formatter(state, exception));

            if (exception is not null)
            {
                context.WriteLine(ConsoleTextColor.Red, exception.ToString());
            }
        }

        private static ConsoleTextColor TextColorFor(LogLevel level) => level switch
        {
            LogLevel.Error or LogLevel.Critical => ConsoleTextColor.Red,
            LogLevel.Warning => ConsoleTextColor.Yellow,
            _ => ConsoleTextColor.White
        };
    }
}
